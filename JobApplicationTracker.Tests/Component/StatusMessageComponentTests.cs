using Bunit;
using JobApplicationTracker.Components.Account.Shared;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace JobApplicationTracker.Tests.Component;

// StatusMessage falls back to reading a cookie off HttpContext, so it always needs one cascaded in
public class StatusMessageComponentTests : BunitContext
{
    [Fact]
    public void RegularMessage_RendersAsSuccessAlert()
    {
        var cut = Render<StatusMessage>(parameters => parameters
            .Add(p => p.Message, "Everything worked!")
            .AddCascadingValue(new DefaultHttpContext()));

        var alert = cut.Find("div.alert");
        Assert.Contains("alert-success", alert.ClassList);
        Assert.Contains("Everything worked!", alert.TextContent);
    }

    [Fact]
    public void MessageStartingWithError_RendersAsDangerAlert()
    {
        var cut = Render<StatusMessage>(parameters => parameters
            .Add(p => p.Message, "Error: something broke")
            .AddCascadingValue(new DefaultHttpContext()));

        var alert = cut.Find("div.alert");
        Assert.Contains("alert-danger", alert.ClassList);
    }

    [Fact]
    public void NoMessage_RendersNothingAtAll()
    {
        var cut = Render<StatusMessage>(parameters => parameters
            .AddCascadingValue(new DefaultHttpContext()));

        Assert.Empty(cut.Markup.Trim());
    }
}
