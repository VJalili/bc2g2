import argparse
import embedder
import os
import sys


def normalize(data_dir):
    pass


def end_to_end(data_dir):
    output_prefix = ""
    classifier_inputs_filename = os.path.join(data_dir, "sampled_graphs.hdf5")
    embedder_inputs_filename = os.path.join(data_dir, "sampled_graphs.hdf5")
    g4ep_train = os.path.join(data_dir, "sampled_graphs_for_edge_predict.hdf5")

    embedder.main(
        data_dir=data_dir,
        graphs_for_classifier=classifier_inputs_filename,
        graphs_to_embed_filename=embedder_inputs_filename,
        graphs_for_train_edge_predictor_filename=g4ep_train,
        graphs_for_val_edge_predictor_filename=g4ep_train,
        graphs_for_eval_edge_predictor_filename=g4ep_train,
        output_prefix=output_prefix)


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

    this_script_abs_path = os.path.dirname(os.path.abspath(__file__))
    data_dir = os.path.join(os.path.dirname(os.path.dirname(this_script_abs_path)), "data")

    if args.commands == "normalize":
        normalize(data_dir)
    elif args.commands == "end-to-end":
        end_to_end(data_dir)
    else:
        print("No command was provided.")
        parser.print_help()


if __name__ == '__main__':
    main()
