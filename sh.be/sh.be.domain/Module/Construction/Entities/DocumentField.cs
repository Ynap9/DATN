using sh.be.shared.Constants.Db;
using sh.be.shared.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sh.be.domain.Module.Construction.Entities
{
    [Table(nameof(DocumentField), Schema = DbSchemas.Construction)]
    public class DocumentField : ISoftDeleted
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }
        public int DocumentTypeId { get; private set; }
        public string Name { get; private set; } = String.Empty;
        public string Field { get; private set; } = String.Empty;
        public string FieldType { get; private set; } = "string";
        public string? FieldImport { get; private set; }
        public bool IsShow { get; private set; }
        public int OrderTableView { get; private set; }
        public int OrderListView { get; private set; }
        public bool IsShowTableView { get; private set; }
        public bool IsShowListView { get; private set; }
        public int? Width { get; private set; }
        public bool IsChargeable { get; private set; }
        public string? FieldNameOcr { get; private set; }
        public bool IsFieldOcr { get; private set; }
        public string? CreatedBy { get; private set; }
        public DateTime? CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool Deleted { get; private set; }
        public string? DeletedBy { get; private set; }

        private DocumentField()
        {
        }

        public static DocumentField Create(int documentTypeId, string name, string field, string? fieldImport, bool isShow,
            bool isChargeable, string? fieldNameOcr, bool isFieldOcr, int order,
            string? createdBy, DateTime? createdDate)
        {
            return new DocumentField
            {
                DocumentTypeId = documentTypeId,
                Name = name,
                Field = field,
                FieldImport = fieldImport,
                IsShow = isShow,
                OrderTableView = order,
                OrderListView = order,
                IsShowTableView = true,
                IsShowListView = true,
                IsChargeable = isChargeable,
                FieldNameOcr = fieldNameOcr,
                IsFieldOcr = isFieldOcr,
                CreatedBy = createdBy,
                CreatedDate = createdDate,
            };
        }

        public void Update(string name, string field, string fieldType, string? fieldImport, bool isShow,
            bool isShowTableView, bool isShowListView, int? width,
            bool isChargeable, string? fieldNameOcr, bool isFieldOcr, string? modifiedBy, DateTime modifiedDate)
        {
            Name = name;
            Field = field;
            FieldType = fieldType;
            FieldImport = fieldImport;
            IsShow = isShow;
            IsShowTableView = isShowTableView;
            IsShowListView = isShowListView;
            Width = width;
            IsChargeable = isChargeable;
            FieldNameOcr = fieldNameOcr;
            IsFieldOcr = isFieldOcr;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void ChangeOrder(int order, string? modifiedBy, DateTime modifiedDate)
        {
            OrderTableView = order;
            OrderListView = order;
            ModifiedBy = modifiedBy;
            ModifiedDate = modifiedDate;
        }

        public void Delete(string? deletedBy, DateTime deletedDate)
        {
            Deleted = true;
            DeletedDate = deletedDate;
            DeletedBy = deletedBy;
        }
    }
}
