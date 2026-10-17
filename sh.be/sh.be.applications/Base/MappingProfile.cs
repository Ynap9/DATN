using AutoMapper;
using sh.be.applications.Module.Construction.Dtos.DocumentField;
using sh.be.applications.Module.Construction.Dtos.DocumentType;
using sh.be.applications.Module.Construction.Dtos.Project;
using sh.be.applications.Module.Construction.Dtos.ProjectConfig;
using sh.be.applications.Base.Dtos.Signing;
using sh.be.applications.Module.DigitalSignature.Dtos.SigningBatch;
using sh.be.applications.Module.DigitalSignature.Dtos.Template;
using sh.be.shared.Constants.Construction;
using sh.be.domain.Module.Construction.Entities;
using sh.be.domain.Module.DigitalSignature.Entities;
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
            CreateMap<Template, GetListTemplateDto>();
            CreateMap<Template, FindTemplateByIdDto>()
                .ForMember(d => d.Positions, o => o.Ignore());
            CreateMap<TemplatePosition, TemplatePositionDto>();
            CreateMap<SigningBatch, FindSigningBatchByIdDto>();
            CreateMap<SigningBatchFile, GetListSigningBatchFileDto>();
            CreateMap<SigningBatch, SigningBatchProgressDto>();
            CreateMap<Template, PdfSigningOptionsDto>()
                .ForMember(dest => dest.StampImage, opt => opt.Ignore())
                .ForMember(dest => dest.WetSignatureImage, opt => opt.Ignore())
                .ForMember(dest => dest.Positions, opt => opt.Ignore());
            CreateMap<TemplatePosition, PdfSigningPositionDto>();
        }
    }
}
