from models_v2 import Node, Edge, Degree, get_engine
from sqlalchemy.sql import func
from sqlalchemy.orm import Session
import base64
import pickle
import sqlalchemy
from sqlalchemy import create_engine, ForeignKey, Table, Column, Integer, String, Float, Time
from sqlalchemy.orm import declarative_base
from sqlalchemy.orm import relationship
from sqlalchemy.exc import OperationalError


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


def compute_node_degree_distribution(engine, in_degree_filename, out_degree_filename):
    with Session(engine) as session:
        print("\n\tGetting in-degree distribution ...")
        query = session.query(Degree.in_degree, func.count(Degree.in_degree)).group_by(Degree.in_degree)
        print("\tWriting the distribution to the file ...")
        with open(in_degree_filename, "w") as f:
            f.write(f"degree\tcount\n")
            for q in query.all():
                f.write(f"{q[0]}\t{q[1]}\n")
        print("\tDone!")

        print("\n\tGetting out-degree distribution ...")
        query = session.query(Degree.out_degree, func.count(Degree.out_degree)).group_by(Degree.out_degree)
        print("\tWriting the distribution to the file ...")
        with open(out_degree_filename, "w") as f:
            f.write(f"degree\tcount\n")
            for q in query.all():
                f.write(f"{q[0]}\t{q[1]}\n")
        print("\tDone!")


def main(in_degree_filename, out_degree_filename, update_node_degrees=True):
    engine = get_engine()
    if update_node_degrees:
        compute_node_degree(engine)

    compute_node_degree_distribution(engine, in_degree_filename, out_degree_filename)
    print("\nAll process finished successfully.")


if __name__ == "__main__":
    main("in_degree_distribution.tsv", "out_degree_distribution.tsv")
