namespace NX_lims_Softlines_Command_System.src.Domain.Share
{

    /// <summary>
    /// 统一结果接口，成功携带数据，失败携带错误信息。
    /// </summary>
    public readonly record struct Result
    {
        //每个使用Result返回的结果都包含一个IsSuccess属性，用于指示操作是否成功
        public bool IsSuccess { get; init; }
        //每个使用Result返回的结果都包含一个Error属性，用于携带操作失败时的错误信息
        public string Error { get; init; }
        public string? ErrorCode { get; init; }
        public IReadOnlyList<string>? ErrorDetails { get; init; }
        public bool IsFailure => !IsSuccess;

        public static Result Ok() => new() { IsSuccess = true };

        public static Result Fail(string error,string? errorCode = null, IReadOnlyList<string>? details = null)
            => new() { IsSuccess = false, Error = error,ErrorCode= errorCode, ErrorDetails = details };

        // 隐式转换，方便 Ok 直接返回 T
        public Result IfSuccess(Func<Result> next) => IsFailure ? this : next();


    }

    /// <summary>
    /// 泛型版本，成功时携带强类型数据。
    /// </summary>
    public readonly record struct Result<T>
    {
        public bool IsSuccess { get; init; }
        public T? Value { get; init; }
        public string Error { get; init; }
        public string? ErrorCode { get; init; }
        public IReadOnlyList<string>? ErrorDetails { get; init; }
        public bool IsFailure => !IsSuccess;

        public static Result<T> Ok(T value) => new() { IsSuccess = true, Value = value };

        public static Result<T> Fail(string error,string? errorCode = null, IReadOnlyList<string>? details = null)
            => new() { IsSuccess = false, Error = error,ErrorCode = errorCode, ErrorDetails = details };

        // 隐式转换，方便 Ok 直接返回 T
        public static implicit operator Result<T>(T value) => Ok(value);

        public Result<TNew> Map<TNew>(Func<T, TNew> mapper) => IsFailure
           ? Result<TNew>.Fail(Error,ErrorCode, ErrorDetails)
           : Result<TNew>.Ok(mapper(Value!));

        public Result<T> Tap(Action<T> sideEffect)
        {
            if (IsSuccess) sideEffect(Value!);
            return this;
        }
    }
}