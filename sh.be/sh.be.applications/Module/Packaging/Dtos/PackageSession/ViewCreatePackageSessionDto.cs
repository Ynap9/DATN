using sh.be.applications.Base.Dtos.DataReader;

namespace sh.be.applications.Module.Packaging.Dtos.PackageSession
{
    public class ViewCreatePackageSessionDto
    {
        public int Id { get; set; }
        public List<ExcelSheetTemplateDto> Sheets { get; set; } = new();
    }
}
