namespace ILearn.Application.Accounts;

public sealed class AccountStorageException(string message, Exception inner) : Exception(message, inner);
public sealed class DuplicateAccountException(Exception inner) : Exception("این مشخصات قبلاً ثبت شده؛ اطلاعات رو بررسی کن.", inner);