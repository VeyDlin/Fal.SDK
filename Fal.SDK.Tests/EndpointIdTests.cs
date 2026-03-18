using Fal.SDK.Http;

namespace Fal.SDK.Tests;


public class EndpointIdTests {
    [Fact]
    public void EnsureFormat_ValidOwnerAlias_ReturnsSame() {
        string id = "fal-ai/fast-sdxl";
        Assert.Equal(id, EndpointId.EnsureFormat(id));
    }


    [Fact]
    public void EnsureFormat_ValidOwnerAliasPath_ReturnsSame() {
        string id = "fal-ai/fast-sdxl/image-to-image";
        Assert.Equal(id, EndpointId.EnsureFormat(id));
    }


    [Fact]
    public void EnsureFormat_InvalidFormat_Throws() {
        Assert.Throws<ArgumentException>(() => EndpointId.EnsureFormat("just-an-id"));
    }


    [Fact]
    public void EnsureFormat_NumericId_ConvertsToSlashFormat() {
        Assert.Equal("1234/test-app", EndpointId.EnsureFormat("1234-test-app"));
    }


    [Fact]
    public void Parse_SimpleOwnerAlias() {
        var parsed = EndpointId.Parse("fal-ai/fast-sdxl");
        Assert.Equal("fal-ai", parsed.owner);
        Assert.Equal("fast-sdxl", parsed.alias);
        Assert.Null(parsed.path);
        Assert.Null(parsed.ns);
    }


    [Fact]
    public void Parse_WithPath() {
        var parsed = EndpointId.Parse("fal-ai/fast-sdxl/image-to-image");
        Assert.Equal("fal-ai", parsed.owner);
        Assert.Equal("fast-sdxl", parsed.alias);
        Assert.Equal("image-to-image", parsed.path);
        Assert.Null(parsed.ns);
    }


    [Fact]
    public void Parse_WithNamespace() {
        var parsed = EndpointId.Parse("workflows/fal-ai/fast-sdxl");
        Assert.Equal("fal-ai", parsed.owner);
        Assert.Equal("fast-sdxl", parsed.alias);
        Assert.Null(parsed.path);
        Assert.Equal("workflows", parsed.ns);
    }


    [Fact]
    public void Parse_ComfyNamespace() {
        var parsed = EndpointId.Parse("comfy/fal-ai/my-workflow");
        Assert.Equal("fal-ai", parsed.owner);
        Assert.Equal("my-workflow", parsed.alias);
        Assert.Equal("comfy", parsed.ns);
    }


    [Fact]
    public void FormatPath_Simple() {
        var id = new EndpointId(owner: "fal-ai", alias: "fast-sdxl");
        Assert.Equal("fal-ai/fast-sdxl", id.FormatPath());
    }


    [Fact]
    public void FormatPath_WithNamespace() {
        var id = new EndpointId(owner: "fal-ai", alias: "fast-sdxl", ns: "workflows");
        Assert.Equal("workflows/fal-ai/fast-sdxl", id.FormatPath());
    }


    [Fact]
    public void FormatPath_WithPath() {
        var id = new EndpointId(owner: "fal-ai", alias: "fast-sdxl", path: "i2i");
        Assert.Equal("fal-ai/fast-sdxl/i2i", id.FormatPath());
    }
}
