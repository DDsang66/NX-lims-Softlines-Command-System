using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Application.Service.BuyerContext
{
    public class BuyerAppService : IScopedDependency
    {
        private readonly IWebHostEnvironment _env;
        private readonly IServerConfig _serverConfig;

        public BuyerAppService(IWebHostEnvironment env, IServerConfig serviceConfig)
        {
            _env = env;
            _serverConfig = serviceConfig;
        }

        /// <summary>
        /// 获取买家手册的所有文件url
        /// </summary>
        /// <returns></returns>
        public List<FileInfoDto> GetManualURLList(string buyer,CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(buyer))
                throw new ArgumentNullException(nameof(buyer));
            //buyer转为首字母大写
            buyer = buyer.Substring(0, 1).ToUpper() + buyer.Substring(1);

            var folderPath = Path.Combine(_env.WebRootPath, "BuyerManual", buyer);

            var resultList = new List<FileInfoDto>();

            if (!Directory.Exists(folderPath))
            {
                // 返回空列表，或者根据业务需求返回 Result.Fail("未找到文件")
                return resultList;
            }

            var files = Directory.GetFiles(folderPath);

            foreach (var filePath in files)
            {
                var fileInfo = new FileInfo(filePath);

                // 获取相对于 WebRootPath 的路径
                var relativePath = Path.GetRelativePath(_env.WebRootPath, filePath);

                // 替换路径分隔符为 URL 标准的分隔符 (/)
                var urlPath = relativePath.Replace(Path.DirectorySeparatorChar, '/');

                var baseUrl = _serverConfig.GetBaseUrl();

                resultList.Add(new FileInfoDto
                {
                    FileName = fileInfo.Name,
                    Url = $"{baseUrl}/{urlPath}", // 加上前导斜杠
                    Size = fileInfo.Length
                });
            }
            return resultList;
        }
    }
}
