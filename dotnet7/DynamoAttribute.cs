using System;

[AttributeUsage(AttributeTargets.Property)]
public class DynamoAttribute : Attribute
{

    public string PropertyName { get; set; }

}