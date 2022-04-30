import numpy as np
import sys


DELIMITER = "\t"


def main(filename):

    # It is inefficient to read the entire file
    # in order to get row and column counts.
    # We could ask these as input arguments, but
    # that is not a good UX. You could leave it
    # as is for now, but hopefully, with migration
    # to DB, all this will be alleviated.
    row_count, col_count = 0, 0
    print(f"\nCounting rows and columns in {filename} ... ", end="", flush=True)
    with open(filename, "r") as f:
        header = next(f)
        col_count = len(header.split(DELIMITER))
        for _ in f:
            row_count += 1

    print(f"Done! #cols={col_count:,} #rows={row_count:,}")

    print(f"\nCreating an empty memory map file (binary) of "
          f"shape=({row_count},{col_count}) ... ", end="", flush=True)
    mem_map_filename = filename + ".dat"
    mem_map = np.memmap(mem_map_filename,
                        dtype='float32',
                        mode='w+',
                        shape=(row_count, col_count))
    print("Done!")
    print(f"\n*** Memory map filename: {mem_map_filename}")

    print("\nPopulating the memory map file:")
    row_counter = 0
    with open(filename, "r") as f:
        next(f)
        for line in f:
            mem_map[row_counter] = line.split(DELIMITER)
            if row_counter % 100000 == 0:
                mem_map.flush()
            row_counter += 1
            print(f"\r\t{row_counter:,} / {row_count:,} ({row_counter / row_count:.2%})", end="")
    mem_map.flush()

    return mem_map_filename, (row_count, col_count)


if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Missing filename.")
        exit()

    out_filename, out_shape = main(sys.argv[1])

    print("\n\n")
    print("*" * 35, "\nAll Process Completed Successfully!")
    print("*" * 35)
    print(f"\nShape: {out_shape}")
    print(f"\nMemoryMap filename: {out_filename}\n")
