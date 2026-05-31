namespace SESport.Core.Domain;

public sealed record EntityRelationship(
   EntityId SubjectEntityId,
   string RelationshipType,
   string TargetName,
   string TargetKind,
   DateOnly? ValidFrom,
   DateOnly? ValidTo,
   IReadOnlyCollection<EntityEvidence> Evidence
);
