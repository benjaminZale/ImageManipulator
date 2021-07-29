namespace ImageManipulator
{
    using System.IO;

    /// <summary>
    /// TaglibSharp does not support using a stream as a source.  This is a Implemenation based on data coming from a <see cref="Stream"/>.
    /// </summary>
    public class StreamFileAbstraction : TagLib.File.IFileAbstraction
    {
        /// <summary>
        /// The internal stream.
        /// </summary>
        private readonly Stream memoryStream;

        /// <summary>
        /// Determines if TagLibSahrp has closed the stream.
        /// </summary>
        private bool isClosed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamFileAbstraction"/> class.
        /// </summary>
        /// <param name="name">The name of the file.  TagLibSharp uses the filename to determine file type.</param>
        /// <param name="stream">The input stream that will be copied into this class.</param>
        public StreamFileAbstraction(string name, Stream stream)
        {
            Name = name;
            long pos = stream.Position;
            memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            stream.Seek(pos, SeekOrigin.Begin);
            memoryStream.Seek(0, SeekOrigin.Begin);
        }

        /// <summary>
        /// Gets the name of the file.  TagLibSharp uses the filename to determine file type.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets a stream for reading.
        /// </summary>
        public Stream ReadStream => memoryStream;

        /// <summary>
        /// Gets a stream can be written to.
        /// </summary>
        public Stream WriteStream
        {
            get
            {
                if (!isClosed)
                {
                    return memoryStream;
                }

                isClosed = false;
                return null;
            }
        }

        /// <summary>
        /// Virtually closes the stream the stream will be "reopened" on the next property access.
        /// </summary>
        /// <param name="stream">The stream to close.</param>
        public void CloseStream(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            isClosed = true;
        }
    }
}
