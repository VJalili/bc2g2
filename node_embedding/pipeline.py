import argparse
import sys


def normalize():
    pass


def end_to_end():
    pass


def main():
    parser = argparse.ArgumentParser(
        prog=sys.argv[0],
        description="This ia helper method that builds on "
                    "individual scripts and enables a single-command "
                    "execution of the pipeline in an end-to-end "
                    "fashion, where it starts from sampling graphs, "
                    "train a model to for node embedding, use the "
                    "model to generate embeddings for a set of nodes, "
                    "and use the embeddings for edge prediction. ")
    subparsers = parser.add_subparsers(dest="commands", help="Commands")

    subparsers.add_parser(
        "normalize",
        help="...")

    subparsers.add_parser(
        "end-to-end", help="Runs the entire pipeline starting from the graph sampling to edge prediction.")

    args = parser.parse_args()

    if args.commands == "normalize":
        normalize()
    elif args.commands == "end-to-end":
        end_to_end()


if __name__ == '__main__':
    main()
