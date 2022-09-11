using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace YamlDotNet.Locations.Tests;

[TestFixture]
public class PerfTests
{
    [Test]
    public void DeserializeAlertRules()
    {
        var toDeserialize = Directory.GetFiles("C:\\dev\\monitoring-rules\\alert_rules", "*.yml", SearchOption.AllDirectories);
        IDeserializer Deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTypeConverter(new StrictEnumConverter())
            .Build();

        var sp = Stopwatch.StartNew();
        foreach (var path in toDeserialize)
        {
            using var reader = File.OpenText(path);
            var parser = new Parser(reader);
            var alertRuleFile = Deserializer.Deserialize<AlertRule>(parser);
        }
        sp.Stop();
        Console.Write(sp.Elapsed);
    }
    
    [Test]
    public void DeserializeAlertRulesWithLocator()
    {
        var toDeserialize = Directory.GetFiles("C:\\dev\\monitoring-rules\\alert_rules", "*.yml", SearchOption.AllDirectories);
       
        var sp = Stopwatch.StartNew();
        foreach (var path in toDeserialize)
        {
            using var reader = File.OpenText(path);
            var parser = new Parser(reader);
            var builder = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithTypeConverter(new StrictEnumConverter());

            var alertRuleFile = builder.Deserialize<AlertRule>(parser);
            //Console.WriteLine(alertRuleFile.locator.GetLocation(x => x.Labels["ops_genie_team_id"]));
            
        }
        sp.Stop();
        Console.Write(sp.Elapsed);
    }
}

public class StrictEnumConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type.IsEnum;
    }
    public object ReadYaml(IParser parser, Type type)
    {
        var s = parser.Consume<YamlDotNet.Core.Events.Scalar>();
        if (!Enum.TryParse(type, s.Value, out var enumValue))
            throw new YamlException($"'{s.Value}' was not recognized as a valid name of the {type.Name} enum type.");
                
        return enumValue;
    }
    public void WriteYaml(IEmitter emitter, object value, Type type)
    {
        throw new NotImplementedException();
    }
}

public class AlertRule
{
    public Dictionary<string, AlertRule> Overrides { get; set; }
    public static readonly Regex ContainsTemplateRegex = new Regex(@"{{\s*([^\s}]*)\s*}}", RegexOptions.Compiled);

    // No need to store in the yaml, these are in the path of the file
    [YamlIgnore] public string Category { get; set; }
    [YamlIgnore] public string Target { get; set; }

    [YamlIgnore] public string OpsGenieTeamId
    {
        get { return Labels?.GetValueOrDefault("ops_genie_team_id"); }
        set { Labels["ops_genie_team_id"] = value; }
    }
    [YamlIgnore] public string TeamName
    {
        get { return Labels?.GetValueOrDefault("team_name", null); }
        set
        {
            if (value != null)
                Labels["team_name"] = value;
        }
    }
    
    [YamlIgnore] public string JiraProject => Labels?.GetValueOrDefault("jira_project");
    // I know the below is getting a little too big brained and hard to read, but I will remove this syntactic sugar when I migrate the rules repo
    [YamlIgnore] public string TsgLink =>  Links?.GetValueOrDefault("TSG") ?? Annotations?.GetValueOrDefault("tsg_link");
    [YamlIgnore] public string DashLink => Links?.GetValueOrDefault("Dash") ?? Annotations?.GetValueOrDefault("dash_link");
    [YamlIgnore] public string SmeSlackChannel => Annotations?.GetValueOrDefault("sme_slack_channel");

    [YamlIgnore] public string AlertName => Category + "." + Target;

    [DefaultValue(null)] // set the default value to null and this will trick yamldotnet to serialize a boolean value of false. noramlly false is the default value and so rules with enabled = false won't serialize properly
    public bool? Enabled { get; set; }
    public List<string> Roles { get; set; }

    [YamlIgnore] public string Interval
    {
        set // we don't support interval changes on prometheus' end atm
        {
        }
        get => "120s";
    }

    public string For { get; set; }
    
    public string Expression { get; set; }
    public string Threshold { get; set; }
    public Dictionary<string, string> Annotations { get; set; }
    public Dictionary<string, string> Labels { get; set; }
    // This should contain all the possible links someone might want to use to troubleshoot an issue
    public Dictionary<string, string> Links { get; set; }
    public DynamicSeverities DynamicSeverity { get; set; }
    public List<string> DynamicSeverityCountLabels { get; set; }
    public Dictionary<string, List<string>> Inhibitions { get; set; }

    public class DynamicSeverities
    {
        public Dictionary<SeverityEnum, string> Duration { get; set; }
        public Dictionary<SeverityEnum, int> Count { get; set; }
        public Dictionary<SeverityEnum, string> Threshold { get; set; }
    }

    public enum SeverityEnum
    {
        NegativeSeverity = -1,
        P0 = 0,
        P1 = 1,
        P2 = 2,
        P3 = 3,
        P4 = 4,
        P5 = 5,
        P6 = 6,
        P7 = 7
    }
    
    /// <summary>
    /// Evaluate type used for alert routing to appropriate Prometheus nodes. Prometheus nodes will include an evaluate type in their alert rule requests
    /// </summary>
    public EvaluateTypes Evaluate { get; set; }
    
    public enum EvaluateTypes
    {
        None = 0,
        PerCluster = 1,
        Globally = 2
    }
}