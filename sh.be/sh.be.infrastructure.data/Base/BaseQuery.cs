using AutoMapper;
using sh.be.infrastructure.data.Persistence.Db;

namespace sh.be.infrastructure.data.Base
{
    public class BaseQuery
    {
        public readonly ShDbContext _shDbContext;
        public readonly IMapper _mapper;

        public BaseQuery(ShDbContext shDbContext, IMapper mapper)
        {
            _shDbContext = shDbContext;
            _mapper = mapper;
        }
    }
}
