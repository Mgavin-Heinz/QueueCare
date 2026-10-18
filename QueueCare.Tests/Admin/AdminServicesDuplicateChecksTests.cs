using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using QueueCare.Controllers;
using QueueCare.Data;
using QueueCare.Models;
using QueueCare.ViewModels;
using Xunit;

namespace QueueCare.Tests.Admin;

// Requirement FR-46 (test case TC-046): the ticket code and the name of a service must be unique.
// These tests call AdminServicesController against an in-memory database, so no SQL Server is needed.
public class AdminServicesDuplicateChecksTests
{
    // The controller only needs TempData to show a message after a successful save.
    private sealed class FakeTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }

    private static ApplicationDbContext NewDatabase()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new ApplicationDbContext(options);
        db.Services.Add(new Service { Name = "General consultation", Code = "G", AvgConsultMinutes = 15, IsActive = true });
        db.Services.Add(new Service { Name = "Chronic medication", Code = "C", AvgConsultMinutes = 10, IsActive = true });
        db.SaveChanges();
        return db;
    }

    private static AdminServicesController NewController(ApplicationDbContext db)
    {
        var http = new DefaultHttpContext();
        return new AdminServicesController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, new FakeTempDataProvider())
        };
    }

    [Fact]
    public async Task Create_CodeAlreadyUsed_IsRefusedAndNothingIsSaved()
    {
        using ApplicationDbContext db = NewDatabase();
        AdminServicesController controller = NewController(db);

        // "g" is stored as "G", which General consultation already uses.
        var model = new ServiceFormViewModel { Name = "Child health", Code = "g", AvgConsultMinutes = 20, IsActive = true };

        IActionResult result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);
        Assert.True(controller.ModelState.ContainsKey(nameof(ServiceFormViewModel.Code)));
        Assert.Equal(2, db.Services.Count());
    }

    [Fact]
    public async Task Create_NameAlreadyUsed_IsRefusedAndNothingIsSaved()
    {
        using ApplicationDbContext db = NewDatabase();
        AdminServicesController controller = NewController(db);

        var model = new ServiceFormViewModel { Name = "General consultation", Code = "X", AvgConsultMinutes = 20, IsActive = true };

        IActionResult result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);
        Assert.True(controller.ModelState.ContainsKey(nameof(ServiceFormViewModel.Name)));
        Assert.Equal(2, db.Services.Count());
    }

    [Fact]
    public async Task Create_NewNameAndCode_IsSavedWithTheCodeInCapitals()
    {
        using ApplicationDbContext db = NewDatabase();
        AdminServicesController controller = NewController(db);

        var model = new ServiceFormViewModel { Name = "Child health", Code = "ch", AvgConsultMinutes = 20, IsActive = true };

        IActionResult result = await controller.Create(model);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(3, db.Services.Count());
        Assert.Contains(db.Services, s => s.Name == "Child health" && s.Code == "CH");
    }

    [Fact]
    public async Task Edit_CodeOfAnotherService_IsRefused()
    {
        using ApplicationDbContext db = NewDatabase();
        AdminServicesController controller = NewController(db);
        Service general = db.Services.Single(s => s.Code == "G");

        // Try to give General consultation the code that Chronic medication uses.
        var model = new ServiceFormViewModel { ServiceId = general.ServiceId, Name = general.Name, Code = "C", AvgConsultMinutes = 15, IsActive = true };

        IActionResult result = await controller.Edit(general.ServiceId, model);

        Assert.IsType<ViewResult>(result);
        Assert.True(controller.ModelState.ContainsKey(nameof(ServiceFormViewModel.Code)));
        Assert.Equal("G", db.Services.Single(s => s.ServiceId == general.ServiceId).Code);
    }

    [Fact]
    public async Task Edit_KeepingItsOwnCodeAndName_IsAllowed()
    {
        using ApplicationDbContext db = NewDatabase();
        AdminServicesController controller = NewController(db);
        Service general = db.Services.Single(s => s.Code == "G");

        // The duplicate check must ignore the service being edited, otherwise nothing could be saved.
        var model = new ServiceFormViewModel { ServiceId = general.ServiceId, Name = general.Name, Code = "g", AvgConsultMinutes = 25, IsActive = true };

        IActionResult result = await controller.Edit(general.ServiceId, model);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(25, db.Services.Single(s => s.ServiceId == general.ServiceId).AvgConsultMinutes);
    }
}
