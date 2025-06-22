using RakNetAgain;

namespace RakNetRunner;

public class Program {
    public static void Main() {
        RakServer server = new(25565);

        Console.WriteLine($"Starting server...");
        server.Start();
    }
}

/*
unconnected pong
ME : 1c  00 00 00 00 00 a8 6e 6d  31 2e cd eb 3d 09 28 2d  00 ff ff 00 fe fe fe fe fd fd fd fd 12 34 56 78  00 5c  4d 43 50 45  3b  56 65 72 79 20 73 65 72 76 65 72  3b  38 31 38  3b  31 2e 32 31 2e 39 32  3b  32 33  3b  39 39  3b  33 32 35 33 38 36 30 38 39 32 33 32 38 39 33 30 38 36 35     3b  4c 65 64 72 6f 63 6b 20 62 65 76 65 6c  3b  43 72 65 61 74 69 76 65  3b  31  3b  31 39 31 33 32  3b  31 39 31 33 33  3b
BDS: 1c  00 00 00 00 00 a4 4d 76  b1 9d df 55 90 b7 b4 48  00 ff ff 00 fe fe fe fe fd fd fd fd 12 34 56 78  00 5e  4d 43 50 45  3b  4e 6f 74 74 20 73 65 72 76 65 72  3b  38 31 38  3b  31 2e 32 31 2e 39 32  3b  30     3b  39 39  3b  31 32 37 39 38 36 33 31 32 37 34 36 34 39 39 39 30 32 31 36  3b  4c 65 64 72 6f 63 6b 20 62 65 76 65 6c  3b  43 72 65 61 74 69 76 65  3b  31  3b  32 35 35 36 35  3b  32 35 35 36 36  3b  30 3b
TODO: server guid needs to be big endian

node:
> Buffer.from([0x31, 0x2e, 0xcd, 0xeb, 0x3d, 0x09, 0x28, 0x2d]).readBigUInt64LE()
3253860892328930865n

> bds = Buffer.from("b1 9d df 55 90 b7 b4 48".split(" ").map(x => parseInt(x, 16)))
<Buffer b1 9d df 55 90 b7 b4 48>
> bds.readBigUInt64LE()
5239014097082621361n
> Buffer.from("31 32 37 39 38 36 33 31 32 37 34 36 34 39 39 39 30 32 31 36".split(" ").map(x => parseInt(x, 16))).toString()
'12798631274649990216'
> bds.readBigUInt64BE() // <-- BE = big endian
12798631274649990216n
>

ME : 1c  00 00 00 00 00 b4 e3 02  2d 28 09 3d eb cd 2e 31  00 ff ff 00 fe fe fe fe fd fd fd fd 12 34 56 78  5e 00  4d 43 50 45 3b 56 65 72 79 20 73 65 72 76 65 72 3b 38 31 38 3b 31 2e 32 31 2e 39 32 3b 32 33 3b 39 39 3b    33 32 35 33 38 36 30 38 39 32 33 32 38 39 33 30 38 36 35    3b 4c 65 64 72 6f 63 6b 20 62 65 76 65 6c 3b 43 72 65 61 74 69 76 65 3b 31 3b 31 39 31 33 32 3b 31 39 31 33 33 3b 30 3b
BDS:
*/
