using MapsterMapper;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service.BuyerContext
{
    public class BuyerQueryAppService:IScopedDependency
    { 
        private readonly IBuyerReposity _buyerRepository;
        private readonly IMapper _mapper;

        public BuyerQueryAppService(IBuyerReposity buyerRepository, IMapper mapper)
        {
            _buyerRepository = buyerRepository;
            _mapper = mapper;
        }
        /// <summary>
        /// 获取买方列表
        /// </summary>
        /// <returns></returns>
        public async Task<List<BuyerListDto>> GetBuyerListAsync(CancellationToken ct) 
        {
            var buyerList = await _buyerRepository.GetBuyerListAsync(ct);

            var dtoList = _mapper.Map<List<BuyerListDto>>(buyerList);

            return dtoList;
        }


    }
}
