using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Woof.Ipc;

namespace Woof.IPC.Tests {

    [TestClass]
    public class UnitTest1 {

        [TestMethod]
        public void AesCryptoCodecTest() {
            using var codecA = new AesCryptoCodec();
            using var codecB = new AesCryptoCodec(codecA.GetKey());
            var prng = new Random();
            var m1 = new byte[8192];
            prng.NextBytes(m1);
            var m2 = new byte[m1.Length];
            Buffer.BlockCopy(m1, 0, m2, 0, m1.Length);
            var c1 = codecA.Encode(m1);
            var c2 = codecA.Encode(m1);
            Assert.IsFalse(c2.SequenceEqual(c1));
            var m1b = codecB.Decode(c1);
            var m2b = codecB.Decode(c2);
            Assert.IsTrue(m1b.SequenceEqual(m1));
            Assert.IsTrue(m2b.SequenceEqual(m1));
            codecB.Apply(ref m2);
            Assert.IsFalse(m2.SequenceEqual(m1));
            codecB.Apply(ref m2, decode: true);
            Assert.IsTrue(m2.SequenceEqual(m1));
        }

        [TestMethod]
        public void AesDeflateCodecTest() {
            using var codecA = new AesDeflateCodec();
            using var codecB = new AesDeflateCodec(codecA.GetKey());
            var m1a = new byte[] { 4, 8, 15, 16, 23, 42 };
            var m2a = new byte[] { 1, 2, 4, 8, 15, 26, 42 };
            var c1 = codecA.Encode(m1a);
            var c2 = codecA.Encode(m2a);
            var m1b = codecB.Decode(c1);
            var m2b = codecB.Decode(c2);
            Assert.IsTrue(m1b.SequenceEqual(m1a));
            Assert.IsTrue(m2b.SequenceEqual(m2a));
            var m1 = new byte[m1a.Length];
            var m2 = new byte[m2a.Length];
            Buffer.BlockCopy(m1a, 0, m1, 0, m1a.Length);
            Buffer.BlockCopy(m2a, 0, m2, 0, m2a.Length);
            codecA.Apply(ref m1);
            codecA.Apply(ref m2);
            codecA.Apply(ref m1, decode: true);
            codecA.Apply(ref m2, decode: true);
            Assert.IsTrue(m1.SequenceEqual(m1a));
            Assert.IsTrue(m2.SequenceEqual(m2a));
        }

        [TestMethod]
        public void NamedPipeChannelTest() {
            var serverCodecs = new IMessageCodec[] { null, new DeflateCodec(), new AesCryptoCodec(), new AesDeflateCodec() };
            var clientCodecs = new IMessageCodec[] { null, new DeflateCodec(), new AesCryptoCodec((serverCodecs[2] as IMessageEncryption).GetKey()), new AesDeflateCodec((serverCodecs[3] as IMessageEncryption).GetKey()) };
            for (var i = 0; i < 4; i++) {
                var serverChannel = new Channel(Channel.Modes.Server, System.IO.Pipes.PipeDirection.InOut, "TEST_IPC", serverCodecs[i]);
                serverChannel.Start();
                serverChannel.DataReceived += (s, e) => {
                    Console.WriteLine(e.Request.ToString());
                    e.Response = "OK";
                };
                Task.Run(() => {
                    using var clientChannel = new Channel(Channel.Modes.Client, System.IO.Pipes.PipeDirection.InOut, "TEST_IPC", clientCodecs[i]);
                    clientChannel.Start();
                    clientChannel.Write((object)"HELLO");
                    Thread.Sleep(1);
                    var responseString = clientChannel.Read().ToString();
                    Console.WriteLine(responseString);
                }).Wait();
                Thread.Sleep(1);
                serverChannel.Dispose();

            }
        }

        [TestMethod]
        public void CombinedChannelTest() {
            var serverChannel = new CombinedChannel(Channel.Modes.Server, "TEST_IPC");
            serverChannel.Start();
            serverChannel.DataReceived += (s, e) => {
                Console.WriteLine(e.Request.ToString());
                e.Response = "OK";
            };
            Task.Run(() => {
                using var clientChannel = new CombinedChannel(Channel.Modes.Client, "TEST_IPC", serverChannel.InitalPipeId);
                clientChannel.Start();
                clientChannel.Write((object)"HELLO");
                Thread.Sleep(1);
                var responseString = clientChannel.Read().ToString();
                Console.WriteLine(responseString);
            }).Wait();
            Thread.Sleep(1);
            serverChannel.Dispose();
        }


    }
}
