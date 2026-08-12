namespace DataAccessor.Repositories;

public enum DeleteResult
{
    Deleted,
    NotFound,
    HasDependencies
}
