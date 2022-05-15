from models import Edge, Degree, get_engine
import math
import matplotlib.pyplot as plt
import pandas as pd
import seaborn as sns
from sqlalchemy.sql import func
from sqlalchemy.orm import Session


def compute_node_degree(engine):
    def update_db(column_name):
        # TODO: this is slow, can we merge all instances in one sqlalchemy query?!
        for q in query.all():
            node_id, degree = q[0], q[1]
            session.merge(Degree(node_id=node_id, **{column_name: degree}), load=True)
            session.commit()
            session.flush()

    with Session(engine) as session:
        print("\tComputing nodes in-degree ... ", end="", flush=True)
        query = session.query(Edge.target_id, func.count(Edge.target_id)).group_by(Edge.target_id)
        print("Done")
        print("\tUpdating database with in-degree ... ", end="", flush=True)
        update_db("in_degree")
        print("Done")

        print("\tComputing nodes out-degree ... ", end="", flush=True)
        query = session.query(Edge.source_id, func.count(Edge.source_id)).group_by(Edge.source_id)
        print("Done")
        print("\tUpdating database with out-degree ... ", end="", flush=True)
        update_db("out_degree")
        print("Done")


def compute_node_degree_distribution(engine, degree_dist_filename):
    with Session(engine) as session:
        dist = {}

        print("\n\tGetting in-degree distribution ...")
        query = session.query(Degree.in_degree, func.count(Degree.in_degree)).group_by(Degree.in_degree)

        for q in query.all():
            degree = q[0]
            count = q[1]
            if degree not in dist:
                dist[degree] = [0, 0]
            dist[degree][0] = count

        print("\n\tGetting out-degree distribution ...")
        query = session.query(Degree.out_degree, func.count(Degree.out_degree)).group_by(Degree.out_degree)

        for q in query.all():
            degree = q[0]
            count = q[1]
            if degree not in dist:
                dist[degree] = [0, 0]
            dist[degree][1] = count

        dist_df = pd.DataFrame([[int(k), v[0], v[1]] for k, v in dist.items()])
        dist_df.rename(columns={0: "Degree", 1: "InDegreeCount", 2: "OutDegreeCount"}, inplace=True)
        dist_df.to_csv(degree_dist_filename, index=False, sep="\t")


def plot_in_out_degree_dist(degree_dist_filename, plot_filename):
    df = pd.read_csv(degree_dist_filename, sep="\t", header=0)
    sns.set_theme()
    sns.set_context("paper")
    sns.set(rc={'figure.figsize': (6, 5)})
    filtered_in_degree = df.loc[df["InDegreeCount"] > 0]
    filtered_out_degree = df.loc[df["OutDegreeCount"] > 0]
    ax = sns.lineplot(data=filtered_in_degree, x="Degree", y="InDegreeCount", label="In-degree")
    ax = sns.lineplot(data=filtered_out_degree, x="Degree", y="OutDegreeCount", label="Out-degree")
    ax.legend()
    ax.set(xscale='log')
    ax.set(yscale='log')
    ax.set_ylabel("Count")
    ax.figure.savefig(plot_filename)


def main(degree_dist_filename, degree_dist_plot_filename, update_node_degrees=True):
    engine = get_engine()
    if update_node_degrees:
        compute_node_degree(engine)

    compute_node_degree_distribution(engine, degree_dist_filename)
    plot_in_out_degree_dist(degree_dist_filename, degree_dist_plot_filename)

    print("\nAll process finished successfully.")


if __name__ == "__main__":
    main("degree_distribution.tsv", "degree_dist.pdf", update_node_degrees=False)
