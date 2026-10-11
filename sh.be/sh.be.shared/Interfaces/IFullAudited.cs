namespace sh.be.shared.Interfaces
{
    public interface IFullAudited : ICreatedBy, IModifiedBy, ISoftDeleted
    {
    }

    public interface ISoftDeleted
    {
        public DateTime? DeletedDate { get; }
        public bool Deleted { get; }
        public string? DeletedBy { get; }
    }

    public interface ICreatedBy
    {
        public DateTime? CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
    }

    public interface IModifiedBy
    {
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
