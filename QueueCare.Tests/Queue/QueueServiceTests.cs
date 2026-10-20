using Microsoft.EntityFrameworkCore;
using QueueCare.Data;
using QueueCare.Models;
using QueueCare.Services;

namespace QueueCare.Tests.Queue;

// Tests for the queue rules (check-in, calling, walk-ins, capacity). They use an in-memory database.
public class QueueServiceTests
{
    private static ApplicationDbContext NewDatabase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    // One service (code G), one clinic day today, one patient with a booked appointment for today.
    private static (Patient patient, Appointment appointment, ClinicDay day) Seed(ApplicationDbContext db, int capacity = 30)
    {
        var service = new Service { Name = "General", Code = "G", AvgConsultMinutes = 15, IsActive = true };
        var day = new ClinicDay
        {
            Service = service,
            Date = DateOnly.FromDateTime(DateTime.Today),
            OpenTime = new TimeOnly(7, 0),
            CloseTime = new TimeOnly(15, 0),
            Capacity = capacity
        };
        var patient = new Patient { FirstName = "Thandi", LastName = "Nkosi", PhoneNumber = "0821234567", CreatedAt = DateTime.Now };
        var appointment = new Appointment
        {
            Patient = patient,
            ClinicDay = day,
            SlotTime = new TimeOnly(9, 0),
            Status = AppointmentStatus.Booked,
            CreatedAt = DateTime.Now
        };
        db.Appointments.Add(appointment);
        db.SaveChanges();
        return (patient, appointment, day);
    }

    [Fact]
    public async Task CheckIn_ByPhone_CreatesTicketAndMarksAppointmentCheckedIn()
    {
        using var db = NewDatabase();
        var (_, appointment, _) = Seed(db);
        var queue = new QueueService(db);

        var result = await queue.CheckInAsync("0821234567");

        Assert.True(result.Ok);
        var entry = await db.QueueEntries.SingleAsync();
        Assert.Equal("G001", entry.TicketNumber);
        Assert.Equal(QueueStatus.Waiting, entry.Status);
        Assert.Equal(AppointmentStatus.CheckedIn, (await db.Appointments.FindAsync(appointment.AppointmentId))!.Status);
    }

    [Fact]
    public async Task CheckIn_Twice_IsRefused()
    {
        using var db = NewDatabase();
        Seed(db);
        var queue = new QueueService(db);

        await queue.CheckInAsync("0821234567");
        var second = await queue.CheckInAsync("0821234567");

        Assert.False(second.Ok);
        Assert.Equal(1, await db.QueueEntries.CountAsync());
    }

    [Fact]
    public async Task CheckIn_UnknownNumber_AsksForWalkIn()
    {
        using var db = NewDatabase();
        Seed(db);
        var queue = new QueueService(db);

        var result = await queue.CheckInAsync("0839999999");

        Assert.False(result.Ok);
        Assert.True(result.NeedsWalkIn);
    }

    [Fact]
    public async Task CallStartServe_FollowsTheRules_AndCompletesTheAppointment()
    {
        using var db = NewDatabase();
        var (_, appointment, _) = Seed(db);
        var queue = new QueueService(db);
        await queue.CheckInAsync("0821234567");
        var id = (await db.QueueEntries.SingleAsync()).QueueEntryId;

        // Can't serve someone who hasn't been called.
        Assert.False((await queue.MarkServedAsync(id)).Ok);

        Assert.True((await queue.CallNextAsync(id)).Ok);
        Assert.True((await queue.StartConsultationAsync(id)).Ok);
        Assert.True((await queue.MarkServedAsync(id)).Ok);

        var entry = await db.QueueEntries.SingleAsync();
        Assert.Equal(QueueStatus.Served, entry.Status);
        Assert.NotNull(entry.CalledAt);
        Assert.NotNull(entry.ServedAt);
        Assert.Equal(AppointmentStatus.Completed, (await db.Appointments.FindAsync(appointment.AppointmentId))!.Status);
    }

    [Fact]
    public async Task NoShow_MarksAppointmentNoShow()
    {
        using var db = NewDatabase();
        var (_, appointment, _) = Seed(db);
        var queue = new QueueService(db);
        await queue.CheckInAsync("0821234567");
        var id = (await db.QueueEntries.SingleAsync()).QueueEntryId;
        await queue.CallNextAsync(id);

        var result = await queue.MarkNoShowAsync(id);

        Assert.True(result.Ok);
        Assert.Equal(AppointmentStatus.NoShow, (await db.Appointments.FindAsync(appointment.AppointmentId))!.Status);
    }

    [Fact]
    public async Task Skip_PutsACalledPatientBackOnTheWaitingList()
    {
        using var db = NewDatabase();
        Seed(db);
        var queue = new QueueService(db);
        await queue.CheckInAsync("0821234567");
        var id = (await db.QueueEntries.SingleAsync()).QueueEntryId;
        await queue.CallNextAsync(id);

        await queue.SkipAsync(id);

        var entry = await db.QueueEntries.SingleAsync();
        Assert.Equal(QueueStatus.Waiting, entry.Status);
        Assert.Null(entry.CalledAt);
    }

    [Fact]
    public async Task WalkIn_CountsTowardsCapacity_AndIsRefusedWhenFull()
    {
        using var db = NewDatabase();
        var (_, _, day) = Seed(db, capacity: 1);   // the booking already uses the only place
        var queue = new QueueService(db);

        var result = await queue.AddWalkInAsync(new WalkInViewModel
        {
            ClinicDayId = day.ClinicDayId,
            FirstName = "Sipho",
            LastName = "Dlamini",
            PhoneNumber = "0711234567"
        });

        Assert.False(result.Ok);
        Assert.Equal(0, await db.QueueEntries.CountAsync());
    }

    [Fact]
    public async Task WalkIn_WithSpace_GetsTheNextTicket()
    {
        using var db = NewDatabase();
        var (_, _, day) = Seed(db);
        var queue = new QueueService(db);
        await queue.CheckInAsync("0821234567");   // takes G001

        var result = await queue.AddWalkInAsync(new WalkInViewModel
        {
            ClinicDayId = day.ClinicDayId,
            FirstName = "Sipho",
            LastName = "Dlamini",
            PhoneNumber = "0711234567"
        });

        Assert.True(result.Ok);
        Assert.Contains(await db.QueueEntries.ToListAsync(), q => q.TicketNumber == "G002" && q.AppointmentId == null);
        Assert.Equal(2, await queue.CountUsedAsync(day.ClinicDayId));
    }
}