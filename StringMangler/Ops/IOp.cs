namespace StringMangler.Ops
{
    /// <summary>
    ///     An Operation.
    /// </summary>
    public interface IOp
    {
        /// <summary>
        ///     Performs the operation represented by this class,
        ///     using the data provided during initialization.
        /// </summary>
        /// <returns>
        ///     Returns <code>true</code> if the operation
        ///     succeeds, <code>false</code> otherwise.
        /// </returns>
        bool PerformOp();
    }
}