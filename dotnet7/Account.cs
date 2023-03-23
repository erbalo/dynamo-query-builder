using Newtonsoft.Json;

public class Account
{

    [DynamoAttribute(PropertyName = "id|org_id")]
    [JsonProperty(PropertyName = "id|org_id")]
    public string IdOrg { get; set; }

    [DynamoAttribute(PropertyName = "status")]
    public string Status { get; set; }

    [DynamoAttribute(PropertyName = "org_id")]
    public string OrgId { get; set; }

    [JsonProperty(PropertyName = "created_at")]
    public string CreatedAt { get; set; }

}
