namespace com.aqua.system
{
    /// <summary>
    /// Result of validation.
    /// </summary>
    public readonly struct ValidationResult
    {
        public bool IsValid { get; }
        public string ErrorMessage { get; }

        private ValidationResult(bool isValid, string errorMessage)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }

        public static ValidationResult Valid() => new ValidationResult(true, null);
        public static ValidationResult Invalid(string message) => new ValidationResult(false, message);
    }
}

