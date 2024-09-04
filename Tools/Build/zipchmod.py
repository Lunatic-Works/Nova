import struct


def read_local_header(f, offset):
    f.seek(offset + 18)
    compressed_size, uncompressed_size, filename_len, extra_len = struct.unpack(
        "<IIHH", f.read(12)
    )
    # filename = f.read(filename_len).decode("utf-8")
    # print("local_header", filename, compressed_size, uncompressed_size)

    f.seek(offset + 30 + filename_len + extra_len + compressed_size)


def read_central_header(f, offset, paths_in_zip):
    new_ext_attributes = b"\x00\x00\xed\x81"

    f.seek(offset + 28)
    filename_len, extra_len, comment_len = struct.unpack("<HHH", f.read(6))
    f.seek(offset + 38)
    (ext_attributes,) = struct.unpack("<I", f.read(4))
    f.seek(offset + 46)
    filename = f.read(filename_len).decode("utf-8")
    # print("central_header", filename, ext_attributes)

    found = False
    if filename in paths_in_zip:
        print(f"Found {filename}")
        found = True
        f.seek(offset + 38)
        f.write(new_ext_attributes)

    f.seek(offset + 46 + filename_len + extra_len + comment_len)
    return found


def zipchmod(zip_filename, paths_in_zip):
    found = 0
    with open(zip_filename, "rb+") as f:
        while True:
            offset = f.tell()
            magic = f.read(4)
            if not magic:
                break
            magic = magic[::-1]
            if magic == b"\x04\x03\x4b\x50":
                read_local_header(f, offset)
            elif magic == b"\x02\x01\x4b\x50":
                found += read_central_header(f, offset, paths_in_zip)
            elif magic == b"\x06\x05\x4b\x50":
                break
            else:
                raise RuntimeError(f"Unknown magic: {magic}")

    if found != len(paths_in_zip):
        raise RuntimeError("Wrong number of paths found")
