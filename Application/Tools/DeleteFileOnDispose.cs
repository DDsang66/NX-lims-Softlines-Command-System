namespace NX_lims_Softlines_Command_System.Application.Tools
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
            if (System.IO.File.Exists(_path))
                System.IO.File.Delete(_path);
        }
    }
}
