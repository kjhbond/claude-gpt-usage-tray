"""Inspect and normalize the PE import names used by portable Windhawk mods."""

from pathlib import Path
from struct import unpack_from

RUNTIME_IMPORTS = {
    "libc++.dll": "libc++.whl",
    "libunwind.dll": "libunwind.whl",
}


def import_name_slots(data):
    """Return (file offset, DLL name) entries from a PE import directory."""
    if data[:2] != b"MZ":
        raise ValueError("Not a PE file: missing MZ header")
    pe = unpack_from("<I", data, 0x3C)[0]
    if data[pe:pe + 4] != b"PE\0\0":
        raise ValueError("Not a PE file: missing PE header")
    sections = unpack_from("<H", data, pe + 6)[0]
    optional_size = unpack_from("<H", data, pe + 20)[0]
    optional = pe + 24
    magic = unpack_from("<H", data, optional)[0]
    if magic not in (0x10B, 0x20B):
        raise ValueError("Unsupported PE optional header")
    directory = optional + (96 if magic == 0x10B else 112)
    import_rva = unpack_from("<I", data, directory + 8)[0]
    if not import_rva:
        raise ValueError("PE import directory is empty")

    section_table = optional + optional_size
    section_rows = []
    for index in range(sections):
        row = section_table + index * 40
        virtual_size, virtual_rva, raw_size, raw_offset = unpack_from("<IIII", data, row + 8)
        section_rows.append((virtual_rva, max(virtual_size, raw_size), raw_offset, raw_size))

    def offset(rva):
        for start, length, raw, raw_size in section_rows:
            delta = rva - start
            if 0 <= delta < length and delta < raw_size:
                return raw + delta
        raise ValueError(f"PE RVA 0x{rva:X} is not backed by file data")

    imports = []
    first = offset(import_rva)
    for index in range(1024):
        descriptor = unpack_from("<IIIII", data, first + index * 20)
        if descriptor == (0, 0, 0, 0, 0):
            break
        name_at = offset(descriptor[3])
        end = data.find(b"\0", name_at, name_at + 256)
        if end < 0:
            raise ValueError("PE import name is unterminated")
        imports.append((name_at, data[name_at:end].decode("ascii")))
    else:
        raise ValueError("PE import descriptor limit exceeded")
    return imports


def verify_windhawk_imports(path):
    names = {name.lower() for _, name in import_name_slots(Path(path).read_bytes())}
    for original, portable in RUNTIME_IMPORTS.items():
        if original in names or portable not in names:
            raise ValueError(f"Windhawk runtime import mismatch: {original} -> {portable}")
    return names


def normalize_windhawk_imports(path):
    path = Path(path)
    data = bytearray(path.read_bytes())
    changes = []
    for at, name in import_name_slots(data):
        portable = RUNTIME_IMPORTS.get(name.lower())
        if portable:
            replacement = portable.encode("ascii")
            if len(replacement) != len(name):
                raise ValueError("Import replacement would change PE layout")
            data[at:at + len(replacement)] = replacement
            changes.append(name)
    if changes:
        path.write_bytes(data)
    verify_windhawk_imports(path)
    return changes
