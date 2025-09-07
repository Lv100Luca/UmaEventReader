using System.Text.Json.Serialization;

namespace UmaEventReader.Model.dto;

public record Root(
    [property: JsonPropertyName("toolKey")]
    string ToolKey,
    [property: JsonPropertyName("headingText")]
    string HeadingText,
    [property: JsonPropertyName("choiceArraySchema")]
    ChoiceArraySchema ChoiceArraySchema
);

public record ChoiceArraySchema(
    [property: JsonPropertyName("relationalDamreyDbSchema")]
    RelationalDamreyDbSchema RelationalDamreyDbSchema,
    [property: JsonPropertyName("choices")]
    List<EventChoice> EventChoices
);

public record RelationalDamreyDbSchema(
    [property: JsonPropertyName("category_relation_id")]
    int? CategoryRelationId,
    [property: JsonPropertyName("category_db_id")]
    int CategoryDbId
);

public record EventChoice(
    [property: JsonPropertyName("id")]
    string Id,
    [property: JsonPropertyName("event_name")]
    string EventName,
    [property: JsonPropertyName("character_name")]
    string CharacterName,
    [property: JsonPropertyName("choice_text")]
    string ChoiceText,
    [property: JsonPropertyName("choice_number")]
    string ChoiceNumber,
    [property: JsonPropertyName("relation")]
    string Relation,
    [property: JsonPropertyName("relation_type")]
    string RelationType,
    [property: JsonPropertyName("success_type")]
    string SuccessType,
    [property: JsonPropertyName("all_outcomes")]
    string AllOutcomes
);