using System;
using ClassicUO.Network.Encryption;
using ClassicUO.Utility;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Network
{
    public class EncryptionTests
    {
        [Theory]
        [InlineData(ClientVersion.CV_OLD)]
        [InlineData(ClientVersion.CV_200)]
        [InlineData(ClientVersion.CV_200X)]
        [InlineData(ClientVersion.CV_500A)]
        [InlineData(ClientVersion.CV_7000)]
        [InlineData(ClientVersion.CV_7090)]
        [InlineData(ClientVersion.CV_7010400)]
        public void Constructor_VariousVersions_DoesNotThrow(ClientVersion version)
        {
            var act = () => new EncryptionHelper(version);

            act.Should().NotThrow();
        }

        [Fact]
        public void Constructor_OldVersion_SetsOldBlowfishEncryption()
        {
            var helper = new EncryptionHelper(ClientVersion.CV_OLD);

            helper.EncryptionType.Should().Be(EncryptionType.OLD_BFISH);
        }

        [Fact]
        public void Constructor_Version200X_SetsBlowfish203()
        {
            var helper = new EncryptionHelper(ClientVersion.CV_200X);

            helper.EncryptionType.Should().Be(EncryptionType.BLOWFISH__2_0_3);
        }

        [Fact]
        public void Constructor_ModernVersion_SetsTwofishMd5()
        {
            var helper = new EncryptionHelper(ClientVersion.CV_7000);

            helper.EncryptionType.Should().Be(EncryptionType.TWOFISH_MD5);
        }

        [Fact]
        public void Constructor_Version200_SetsBlowfish()
        {
            var helper = new EncryptionHelper(ClientVersion.CV_200);

            helper.EncryptionType.Should().Be(EncryptionType.BLOWFISH);
        }

        [Fact]
        public void Initialize_LoginMode_DoesNotThrow()
        {
            var helper = new EncryptionHelper(ClientVersion.CV_500A);

            var act = () => helper.Initialize(true, 0x12345678);

            act.Should().NotThrow();
        }

        [Fact]
        public void Initialize_GameMode_DoesNotThrow()
        {
            var helper = new EncryptionHelper(ClientVersion.CV_7000);

            var act = () => helper.Initialize(false, 0x12345678);

            act.Should().NotThrow();
        }
    }

    public class LoginCryptBehaviourTests
    {
        [Fact]
        public void Initialize_DoesNotThrow()
        {
            var loginCrypt = new LoginCryptBehaviour();

            var act = () => loginCrypt.Initialize(0xDEADBEEF, 0x11111111, 0x22222222, 0x33333333);

            act.Should().NotThrow();
        }

        [Fact]
        public void Encrypt_NonZeroInput_ProducesNonZeroOutput()
        {
            var loginCrypt = new LoginCryptBehaviour();
            loginCrypt.Initialize(0xDEADBEEF, 0x11111111, 0x22222222, 0x33333333);

            var src = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
            var dst = new byte[8];

            loginCrypt.Encrypt(src, dst, src.Length);

            // The encrypted output should differ from all zeros (encryption XORs with key)
            dst.Should().NotBeEquivalentTo(new byte[8]);
        }

        [Fact]
        public void Encrypt_ProducesDifferentOutputFromInput()
        {
            var loginCrypt = new LoginCryptBehaviour();
            loginCrypt.Initialize(0xDEADBEEF, 0x11111111, 0x22222222, 0x33333333);

            var src = new byte[] { 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48 };
            var dst = new byte[8];

            loginCrypt.Encrypt(src, dst, src.Length);

            dst.Should().NotBeEquivalentTo(src);
        }

        [Fact]
        public void Encrypt_SameSeedAndKeys_IsDeterministic()
        {
            var crypt1 = new LoginCryptBehaviour();
            crypt1.Initialize(0xDEADBEEF, 0x11111111, 0x22222222, 0x33333333);

            var crypt2 = new LoginCryptBehaviour();
            crypt2.Initialize(0xDEADBEEF, 0x11111111, 0x22222222, 0x33333333);

            var src = new byte[] { 0x10, 0x20, 0x30, 0x40 };
            var dst1 = new byte[4];
            var dst2 = new byte[4];

            crypt1.Encrypt(src, dst1, src.Length);
            crypt2.Encrypt(src, dst2, src.Length);

            dst1.Should().BeEquivalentTo(dst2);
        }

        [Fact]
        public void Encrypt_OLD_ProducesDifferentOutputFromInput()
        {
            var loginCrypt = new LoginCryptBehaviour();
            loginCrypt.Initialize(0xCAFEBABE, 0xAAAAAAAA, 0xBBBBBBBB, 0xCCCCCCCC);

            var src = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var dst = new byte[4];

            loginCrypt.Encrypt_OLD(src, dst, src.Length);

            dst.Should().NotBeEquivalentTo(src);
        }

        [Fact]
        public void Encrypt_1_25_36_ProducesDifferentOutputFromInput()
        {
            var loginCrypt = new LoginCryptBehaviour();
            loginCrypt.Initialize(0xCAFEBABE, 0xAAAAAAAA, 0xBBBBBBBB, 0xCCCCCCCC);

            var src = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var dst = new byte[4];

            loginCrypt.Encrypt_1_25_36(src, dst, src.Length);

            dst.Should().NotBeEquivalentTo(src);
        }

        [Fact]
        public void Encrypt_DifferentSeeds_ProduceDifferentOutput()
        {
            var crypt1 = new LoginCryptBehaviour();
            crypt1.Initialize(0x11111111, 0xAAAAAAAA, 0xBBBBBBBB, 0xCCCCCCCC);

            var crypt2 = new LoginCryptBehaviour();
            crypt2.Initialize(0x22222222, 0xAAAAAAAA, 0xBBBBBBBB, 0xCCCCCCCC);

            var src = new byte[] { 0x10, 0x20, 0x30, 0x40 };
            var dst1 = new byte[4];
            var dst2 = new byte[4];

            crypt1.Encrypt(src, dst1, src.Length);
            crypt2.Encrypt(src, dst2, src.Length);

            dst1.Should().NotBeEquivalentTo(dst2);
        }
    }
}
