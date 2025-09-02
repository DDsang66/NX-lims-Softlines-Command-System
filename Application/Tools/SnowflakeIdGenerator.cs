namespace NX_lims_Softlines_Command_System.Application.Tools
{
    public class SnowflakeIdGenerator
    {
        private const long Twepoch = 1288834974657L;
        private const long WorkerIdBits = 5L;
        private const long DataCenterIdBits = 5L;
        private const long MaxWorkerId = -1L ^ -1L << (int)WorkerIdBits;
        private const long MaxDataCenterId = -1L ^ -1L << (int)DataCenterIdBits;
        private const long SequenceBits = 12L;
        private const long WorkerIdShift = SequenceBits;
        private const long DataCenterIdShift = SequenceBits + WorkerIdBits;
        private const long TimestampLeftShift = SequenceBits + WorkerIdBits + DataCenterIdBits;
        private const long SequenceMask = -1L ^ -1L << (int)SequenceBits;

        private long _workerId;
        private long _dataCenterId;
        private long _sequence = 0L;
        private long _lastTimestamp = -1L;

        public SnowflakeIdGenerator(long workerId = 0L, long dataCenterId = 0L)
        {
            if (workerId > MaxWorkerId || workerId < 0)
                throw new ArgumentException($"worker Id can't be greater than {MaxWorkerId} or less than 0");
            if (dataCenterId > MaxDataCenterId || dataCenterId < 0)
                throw new ArgumentException($"data center Id can't be greater than {MaxDataCenterId} or less than 0");
            _workerId = workerId;
            _dataCenterId = dataCenterId;
        }

        public long NextId()
        {
            long timestamp = TimeGen();
            if (timestamp < _lastTimestamp)
            {
                throw new Exception($"Clock moved backwards.  Refusing to generate id for {_lastTimestamp - timestamp} milliseconds");
            }
            if (_lastTimestamp == timestamp)
            {
                _sequence = (_sequence + 1) & SequenceMask;
                if (_sequence == 0)
                {
                    timestamp = TilNextMillis(_lastTimestamp);
                }
            }
            else
            {
                _sequence = 0L;
            }
            _lastTimestamp = timestamp;

            // 显式类型转换为 int
            return ((timestamp - Twepoch) << (int)TimestampLeftShift) |
                   ((int)_dataCenterId << (int)DataCenterIdShift) |
                   ((int)_workerId << (int)WorkerIdShift) |
                   (int)_sequence;
        }

        private long TilNextMillis(long lastTimestamp)
        {
            long timestamp = TimeGen();
            while (timestamp <= lastTimestamp)
            {
                timestamp = TimeGen();
            }
            return timestamp;
        }

        private long TimeGen()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }
    }
}
