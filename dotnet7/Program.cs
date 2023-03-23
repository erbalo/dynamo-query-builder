using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;

public class Program
{

    static async Task Main(string[] args)
    {
        var dynamoDBClient = new AmazonDynamoDBClient();

        var queryBuilder = new QueryBuilder<Account>(dynamoDBClient)
            .WithIndexName("accounts-org_id-created_at-GSI")
            .AddKeyCondition(a => a.OrgId, ComparisonOperator.EQ, "GSI_ID")
            .AddFilterCondition(a => a.Status, ExpressionOperator.GreaterThan, "DELETED")
            .Limit(10);


        var results = await queryBuilder.ExecuteQueryAsync("TABLE_NAME");

        foreach (var item in results)
        {
            Console.WriteLine($"Id: {item.IdOrg} with status {item.Status} created at: {item.CreatedAt}");
        }

    }
}
