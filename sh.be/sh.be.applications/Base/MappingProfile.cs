using AutoMapper;
using sh.be.applications.Module.Construction.Dtos.DocumentField;
using sh.be.applications.Module.Construction.Dtos.DocumentType;
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
        }
    }
}
