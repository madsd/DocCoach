namespace DocCoach.Web.Models;

/// <summary>
/// Defines the type of a configuration parameter for UI rendering.
/// </summary>
public enum ConfigParameterType
{
    /// <summary>Integer value (rendered as number input or slider).</summary>
    Integer,
    
    /// <summary>Decimal value (rendered as number input).</summary>
    Decimal,
    
    /// <summary>Text value (rendered as text input).</summary>
    String,
    
    /// <summary>Boolean value (rendered as switch/checkbox).</summary>
    Boolean,
    
    /// <summary>Selection from predefined options (rendered as dropdown).</summary>
    Enum,
    
    /// <summary>Multi-line text (rendered as textarea).</summary>
    Text,
    
    /// <summary>Percentage value 0-100 (rendered as slider).</summary>
    Percentage
}

/// <summary>
/// Describes a configuration parameter that an analyzer accepts.
/// Used to dynamically generate configuration UI in the admin panel.
/// </summary>
/// <param name="Name">JSON property name (e.g., "maxWords").</param>
/// <param name="DisplayName">Human-readable label (e.g., "Maximum Words").</param>
/// <param name="Description">Help text explaining the parameter.</param>
/// <param name="Type">The parameter data type for UI rendering.</param>
/// <param name="DefaultValue">Default value when not specified.</param>
/// <param name="MinValue">Minimum allowed value (for numeric types).</param>
/// <param name="MaxValue">Maximum allowed value (for numeric types).</param>
/// <param name="IsRequired">Whether the parameter must be provided.</param>
/// <param name="Options">For Enum type: list of valid option values.</param>
public record ConfigParameter(
    string Name,
    string DisplayName,
    string Description,
    ConfigParameterType Type,
    object? DefaultValue = null,
    object? MinValue = null,
    object? MaxValue = null,
    bool IsRequired = false,
    IReadOnlyList<string>? Options = null
)
{
    /// <summary>
    /// Creates an integer parameter with range constraints.
    /// </summary>
    public static ConfigParameter Integer(
        string name,
        string displayName,
        string description,
        int defaultValue,
        int? min = null,
        int? max = null,
        bool required = false) => new(
            name, displayName, description,
            ConfigParameterType.Integer,
            defaultValue, min, max, required);
    
    /// <summary>
    /// Creates a decimal parameter with range constraints.
    /// </summary>
    public static ConfigParameter Decimal(
        string name,
        string displayName,
        string description,
        decimal defaultValue,
        decimal? minValue = null,
        decimal? maxValue = null,
        bool required = false) => new(
            name, displayName, description,
            ConfigParameterType.Decimal,
            defaultValue, minValue, maxValue, required);
    
    /// <summary>
    /// Creates a percentage parameter (0-100 range).
    /// </summary>
    public static ConfigParameter Percentage(
        string name,
        string displayName,
        string description,
        int defaultValue,
        bool required = false) => new(
            name, displayName, description,
            ConfigParameterType.Percentage,
            defaultValue, 0, 100, required);
    
    /// <summary>
    /// Creates a boolean parameter.
    /// </summary>
    public static ConfigParameter Boolean(
        string name,
        string displayName,
        string description,
        bool defaultValue = false) => new(
            name, displayName, description,
            ConfigParameterType.Boolean,
            defaultValue);
    
    /// <summary>
    /// Creates a string parameter.
    /// </summary>
    public static ConfigParameter String(
        string name,
        string displayName,
        string description,
        string? defaultValue = null,
        bool required = false) => new(
            name, displayName, description,
            ConfigParameterType.String,
            defaultValue, IsRequired: required);
    
    /// <summary>
    /// Creates a multi-line text parameter.
    /// </summary>
    public static ConfigParameter Text(
        string name,
        string displayName,
        string description,
        string? defaultValue = null) => new(
            name, displayName, description,
            ConfigParameterType.Text,
            defaultValue);
    
    /// <summary>
    /// Creates an enum/dropdown parameter.
    /// </summary>
    public static ConfigParameter Enum(
        string name,
        string displayName,
        string description,
        IReadOnlyList<string> options,
        string? defaultValue = null,
        bool required = false) => new(
            name, displayName, description,
            ConfigParameterType.Enum,
            defaultValue, Options: options, IsRequired: required);
}
