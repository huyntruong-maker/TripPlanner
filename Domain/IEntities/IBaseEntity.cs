namespace Domain.IEntities;

public interface IBaseEntity
{
    Guid CreatedBy { get; set; }

    DateTimeOffset CreatedAt { get; set; }

    Guid UpdatedBy { get; set; }

    DateTimeOffset UpdatedAt { get; set; }
}