namespace ImageManipulator
{
    /// <summary>
    /// The kind of action to perform on the image.
    /// </summary>
    internal enum ActionKind
    {
        /// <summary>
        /// Rotate clockwise.
        /// </summary>
        Clockwise,

        /// <summary>
        /// Rotate counter clockwise.
        /// </summary>
        CounterClockwise,

        /// <summary>
        /// Flip the image.
        /// </summary>
        Flip,

        /// <summary>
        /// Mirror the image.
        /// </summary>
        Mirror,
    }
}
