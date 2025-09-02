namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService
{
    internal sealed class DeleteFileOnDispose : IDisposable
    {

        private readonly string _path;
        public DeleteFileOnDispose(string path)
        {
            _path = path;
        }
        public void Dispose()
        {
            if (File.Exists(_path))
                File.Delete(_path);
        }
    }
}
