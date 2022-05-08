import h5py
import networkx as nx
import os
import pandas as pd
from pathlib import Path
import seaborn as sns
import sys


def get_graph_stats(pair_indices):
    g = nx.Graph()
    for pair in pair_indices:
        g.add_edge(pair[0], pair[1])

    node_count = len(g.nodes)
    edge_count = len(g.edges)
    radius = nx.radius(g) if nx.is_connected(g) else 0

    return node_count, edge_count, radius


def plot_graph_stats(stats_filename):
    stats_df = pd.read_csv(stats_filename, sep="\t")

    sns.set_theme()
    sns.set_context("paper")

    ax = sns.histplot(data=stats_df, x="node_count", hue="group", element="step", log_scale=True)
    ax.figure.savefig(os.path.splitext(stats_filename)[0] + "_node_count.pdf")
    ax.clear()

    ax = sns.histplot(data=stats_df, x="edge_count", hue="group", element="step", log_scale=True)
    ax.figure.savefig(os.path.splitext(stats_filename)[0] + "_edge_count.pdf")
    ax.clear()

    ax = sns.histplot(data=stats_df, x="radius", hue="group", element="step")
    ax.figure.savefig(os.path.splitext(stats_filename)[0] + "_radius.pdf")
    ax.clear()


def main(filename, output_dir, groups=None, output_prefix=""):
    stats = []
    groups = groups or ["graph", "random_edges"]
    with h5py.File(filename, "r") as f:
        for idx in f.keys():
            for group_key in groups:
                group = f[idx][group_key]
                pair_indices = group["pair_indices"][...]
                stats.append([group_key, *get_graph_stats(pair_indices)])

    stats_df = pd.DataFrame(stats, columns=["group", "node_count", "edge_count", "radius"])

    stats_filename = os.path.join(output_dir, output_prefix + Path(filename).stem + "_stats.tsv")
    print(f"Writing stats to {stats_filename}")
    stats_df.to_csv(stats_filename, sep="\t", index=False)
    plot_graph_stats(stats_filename)


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Missing hdf5 filename.")
        exit()
    main(sys.argv[1], ".")
