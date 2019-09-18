using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Woof.Ipc {

    /// <summary>
    /// Command line arguments collection class.
    /// </summary>
    public class ProcessArguments : IEnumerable<string> {

        #region Indexers

        /// <summary>
        /// Gets or sets argument value specified with its index.
        /// </summary>
        /// <param name="i">Zero based collection index.</param>
        /// <returns>Argument value.</returns>
        public string this[int i] {
            get => Items[i];
            set => Items[i] = value;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returs arguments collection length.
        /// </summary>
        public int Length => Items?.Length ?? 0;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates new command line arguments collection.
        /// </summary>
        /// <param name="arguments">Unquoted arguments.</param>
        public ProcessArguments(params string[] arguments) => Items = arguments;

        #endregion

        #region Methods

        /// <summary>
        /// Serializes command line arguments collection with necessary character quoting.
        /// </summary>
        /// <returns>Serialized command line arguments string.</returns>
        public override string ToString() {
            if (Items == null) return null;
            StringBuilder b = new StringBuilder();
            for (int i = 0; i < Items.Length; i++) {
                if (i > 0) b.Append(' ');
                AppendArgument(b, Items[i]);
            }
            return b.ToString();
        }

        /// <summary>
        /// Quotes argument string and appends it to specified <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="b"><see cref="StringBuilder"/> object the quoted argument will be appended to.</param>
        /// <param name="arg">Unquoted argument value.</param>
        void AppendArgument(StringBuilder b, string arg) {
            if (arg.Length > 0 && arg.IndexOfAny(ArgQuoteChars) < 0) {
                b.Append(arg);
            }
            else {
                b.Append('"');
                for (int j = 0; ; j++) {
                    int backslashCount = 0;
                    while (j < arg.Length && arg[j] == '\\') {
                        backslashCount++;
                        j++;
                    }
                    if (j == arg.Length) {
                        b.Append('\\', backslashCount * 2);
                        break;
                    }
                    else if (arg[j] == '"') {
                        b.Append('\\', backslashCount * 2 + 1);
                        b.Append('"');
                    }
                    else {
                        b.Append('\\', backslashCount);
                        b.Append(arg[j]);
                    }
                }
                b.Append('"');
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)Items).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<string>)Items).GetEnumerator();

        #endregion

        #region Private data

        /// <summary>
        /// Argument values.
        /// </summary>
        readonly string[] Items;

        /// <summary>
        /// Characters which must be quoted in argument strings.
        /// </summary>
        static readonly char[] ArgQuoteChars = { ' ', '\t', '\n', '\v', '"' };

        #endregion

    }

}