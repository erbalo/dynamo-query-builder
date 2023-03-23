using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using System;
using System.Reflection;
using System.Linq.Expressions;
using Newtonsoft.Json;
using System.Text;
using Amazon.DynamoDBv2.DataModel;

class QueryBuilder<T>
{

    private readonly IAmazonDynamoDB dynamoDBClient;

    private string indexName;
    private int limit;
    private Dictionary<string, Condition> keyConditions;
    private StringBuilder filterExpression;
    private Dictionary<string, string> expressionAttributeNames;
    private Dictionary<string, AttributeValue> expressionAttributeValues;

    public QueryBuilder(IAmazonDynamoDB dynamoDBClient)
    {
        this.dynamoDBClient = dynamoDBClient;

        this.keyConditions = new Dictionary<string, Condition>();
        this.filterExpression = new StringBuilder();
        this.expressionAttributeNames = new Dictionary<string, string>();
        this.expressionAttributeValues = new Dictionary<string, AttributeValue>();
        this.limit = 20;
    }

    public QueryBuilder<T> WithIndexName(string indexName)
    {
        this.indexName = indexName;
        return this;
    }

    public QueryBuilder<T> Limit(int limit)
    {
        this.limit = limit;
        return this;
    }

    public QueryBuilder<T> AddKeyCondition(Expression<Func<T, object>> propertyExpression, ComparisonOperator comparisonOperator, object value)
    {
        string propertyName = GetPropertyName(propertyExpression);
        var attributeValueList = new List<AttributeValue> { new AttributeValue { S = value.ToString() } };

        var conditionToAdd = new Condition
        {
            ComparisonOperator = comparisonOperator,
            AttributeValueList = attributeValueList
        };

        this.keyConditions.Add(propertyName, conditionToAdd);
        return this;
    }


    public QueryBuilder<T> AddFilterCondition(Expression<Func<T, object>> propertyExpression, string expressionOperator, object value, string logicalOperator = null)
    {
        if (filterExpression.Length > 0 && string.IsNullOrEmpty(logicalOperator))
        {
            throw new ArgumentException("Logical operator (AND/OR) must be specified for additional filter conditions.");
        }

        string propertyName = GetPropertyName(propertyExpression);
        string attributeName = "#" + propertyName;
        string attributeValue = ":" + propertyName;

        this.expressionAttributeNames.Add(attributeName, propertyName);
        this.expressionAttributeValues.Add(attributeValue, new AttributeValue { S = value.ToString() });

        if (!string.IsNullOrEmpty(logicalOperator))
        {
            this.filterExpression.Append($" {logicalOperator} ");
        }

        this.filterExpression.Append($"{attributeName} {expressionOperator} {attributeValue}");
        return this;
    }


    public async Task<List<T>> ExecuteQueryAsync(string tableName)
    {
        Console.WriteLine($"CONDITIONS > {JsonConvert.SerializeObject(keyConditions)}");
        Console.WriteLine($"ATTRIBUTE NAMES > {JsonConvert.SerializeObject(expressionAttributeNames)}");
        Console.WriteLine($"ATTRIBUTE VALUES > {JsonConvert.SerializeObject(expressionAttributeValues)}");
        Console.WriteLine($"FILTERS > {JsonConvert.SerializeObject(filterExpression.ToString())}");

        var queryRequest = new QueryRequest
        {
            TableName = tableName,
            IndexName = this.indexName,
            KeyConditions = this.keyConditions,
            FilterExpression = this.filterExpression.ToString(),
            ExpressionAttributeNames = this.expressionAttributeNames,
            ExpressionAttributeValues = this.expressionAttributeValues,
            Limit = 10
        };

        var response = await this.dynamoDBClient.QueryAsync(queryRequest);
        //Console.WriteLine($"RES > {JsonConvert.SerializeObject(response.Items)}");
        Console.WriteLine($"Count items {response.Items.Count}");

        var results = new List<T>();

        foreach (var item in response.Items)
        {

            var document = Document.FromAttributeMap(item);
            var json = document.ToJson();
            var result = JsonConvert.DeserializeObject<T>(json);
            results.Add(result);
        }

        return results;
    }

    private static string GetPropertyName(Expression<Func<T, object>> propertyExpression)
    {
        if (propertyExpression.Body is MemberExpression memberExpression)
        {
            return GetAttributeNameOrPropertyName(memberExpression);
        }
        else if (propertyExpression.Body is UnaryExpression unaryExpression &&
                unaryExpression.Operand is MemberExpression unaryMemberExpression)
        {
            return GetAttributeNameOrPropertyName(unaryMemberExpression);
        }

        throw new ArgumentException("Invalid property expression", nameof(propertyExpression));
    }

    private static string GetAttributeNameOrPropertyName(MemberExpression memberExpression)
    {
        var property = memberExpression.Member as PropertyInfo;
        var attribute = property?.GetCustomAttribute<DynamoAttribute>();

        return attribute?.PropertyName ?? memberExpression.Member.Name;
    }
}