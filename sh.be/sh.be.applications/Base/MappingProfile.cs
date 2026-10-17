using AutoMapper;
using sh.be.applications.Module.Construction.Dtos.DocumentField;
using sh.be.applications.Module.Construction.Dtos.DocumentType;
using sh.be.applications.Module.Construction.Dtos.Project;
using sh.be.applications.Module.Construction.Dtos.ProjectConfig;
using sh.be.shared.Constants.Construction;
using sh.be.domain.Module.Construction.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.applications.Base
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Add your mapping configurations here
            CreateMap<DocumentType, GetListDocumentTypeDto>();
            CreateMap<DocumentType, FindDocumentTypeByIdDto>();
            CreateMap<DocumentType, GetListDocumentTypeDropDownDto>();
            CreateMap<DocumentField, GetListDocumentFieldDto>();
            CreateMap<DocumentField, FindDocumentFieldByIdDto>();
            CreateMap<DocumentField, GetListDocumentFieldDropDownDto>();
            CreateMap<DocumentField, FieldImportHeaderDto>()
                .ForMember(d => d.Headers, o => o.Ignore());
            CreateMap<Project, GetListProjectDto>();
            CreateMap<Project, FindProjectByIdDto>()
                .ForMember(d => d.DocumentTypeIds, o => o.Ignore());
            CreateMap<Project, GetListProjectDropDownDto>();
            CreateMap<ProjectConfig, GetListProjectConfigDto>()
                .ForMember(d => d.Value, o => o.MapFrom(s => ProjectConfigConst.SECRET_NAMES.Contains(s.Name) ? ProjectConfigConst.MASKED_VALUE : s.Value));
            CreateMap<ProjectConfig, FindProjectConfigByIdDto>()
                .ForMember(d => d.Value, o => o.MapFrom(s => ProjectConfigConst.SECRET_NAMES.Contains(s.Name) ? ProjectConfigConst.MASKED_VALUE : s.Value));
        }
    }
}
