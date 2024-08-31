using Tomlyn;
using System;
using System.Net;

namespace My.Tests;

class MyConfig
{
    [System.ComponentModel.DataAnnotations.Required]
    public string? config_one { get; set; }
    [System.ComponentModel.DataAnnotations.Required]
    public int config_two { get; set; }
    [System.ComponentModel.DataAnnotations.Required]
    public IPAddress? config_three { get; set; }
}

class YamlMyConfig
{
    public required string config_one { get; set; }
    public required int config_two { get; set; }
    public required IPAddress config_three { get; set; }
}

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var toml = @"
config_one = ""this is a string""
# This is a comment of a table
#config_two = 1 # Comment a key
config_three = ""127.0.0.1""
";

        TomlModelOptions options = new TomlModelOptions();
        options.ConvertToModel = (object value, Type type) =>
        {
            if (type == typeof(IPAddress) && value is string)
            {
                return IPAddress.Parse((value as string)!);
            }
            return value;
        };
        // Parse the TOML string to the default runtime model `TomlTable`
        var model = Toml.ToModel<MyConfig>(toml, options: options);
        Assert.Equal("this is a string", model.config_one);
        // This is the problem!!, it doesn't validate the model entirely
        Assert.NotEqual(1, model.config_two);
        Assert.Equal(IPAddress.Parse("127.0.0.1"), model.config_three);


        var yaml = @"
config_one: this is a string
config_two: 1
config_three: 127.0.0.1";

        // Ooh, does ip addresses too without special converter
        var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
            // And does validation
            .WithEnforceRequiredMembers()
            .Build();
        var yamlmodel = deserializer.Deserialize<YamlMyConfig>(yaml);
        var context = new System.ComponentModel.DataAnnotations.ValidationContext(model, null, null);
        System.ComponentModel.DataAnnotations.Validator.ValidateObject(model, context, validateAllProperties: true);

        Assert.Equal("this is a string", yamlmodel.config_one);
        Assert.Equal(1, yamlmodel.config_two);
        Assert.Equal(IPAddress.Parse("127.0.0.1"), yamlmodel.config_three);

    }
}
// 