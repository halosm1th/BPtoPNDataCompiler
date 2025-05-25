namespace BPtoPNDataCompiler;

public class UpdateDetail<T>
{
    /// <summary>
    /// Initializes a new instance of the UpdateDetail class.
    /// </summary>
    /// <param name="entry">The data entry object.</param>
    /// <param name="fieldName">The name of the field being changed.</param>
    /// <param name="oldValue">The original value of the field.</param>
    /// <param name="newValue">The new value for the field.</param>
    public UpdateDetail(T entry, string fieldName, string? oldValue, string? newValue)
    {
        Entry = entry;
        FieldName = fieldName;
        OldValue = oldValue;
        NewValue = newValue;
    }

    public T Entry { get; } // The actual entry object that needs to be updated
    public string FieldName { get; } // The name of the field/property being changed
    public string? OldValue { get; } // The original value of the field before the change
    public string? NewValue { get; } // The new value that the field should be updated to

    public override string ToString()
    {
        return
            $"Entry Type: {typeof(T).Name}, Field: {FieldName}, Old: '{OldValue ?? "NULL"}', New: '{NewValue ?? "NULL"}'";
    }
}