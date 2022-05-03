"""
Populates a PostgreSQL database with nodes and edges given TSV format.
"""

import sys
from models import get_engine, Node, Edge, BlockStatus
from sqlalchemy.orm import Session

DELIMITER = "\t"


def main(nodes_filename, edges_filename, block_status_filename):
    engine = get_engine()
    with Session(engine) as session:
        counter = 0
        print(f"\nPopulating nodes from {nodes_filename}:", flush=True)
        with open(nodes_filename, "r") as f:
            next(f)  # skip header
            for line in f:
                cols = line.strip().split(DELIMITER)
                res = session.query(Node).filter(Node.id_generated == int(cols[0])).all()
                if len(res) == 0:
                    session.add(Node(id_generated=int(cols[0]),
                                     script_type=int(cols[1])))
                    session.commit()
                    session.flush()

                counter += 1
                if counter % 1000 == 0:
                    print(f"\r\tNodes added: {counter:,}", end="", flush=True)
                if counter == 1000000:
                    break

        print("\r\tFlushing transaction to database ...", end="", flush=True)
        session.commit()
        print(f"\r\tFinished adding {counter:,} nodes.")

        counter = 0
        print(f"\n\nPopulating edges from {edges_filename}:", flush=True)
        with open(edges_filename, "r") as f:
            next(f)  # skip header
            for line in f:
                cols = line.strip().split(DELIMITER)
                try:
                    session.add(Edge(source_id=int(cols[0]),
                                     target_id=int(cols[1]),
                                     value=float(cols[2]),
                                     edge_type=float(cols[3]),
                                     time_offset=float(cols[4]),
                                     block_height=float(cols[5])))
                    session.commit()
                    session.flush()
                except Exception:
                    # TODO: this indicates a problem with the source data, maybe better log it instead.
                    pass

                counter += 1
                if counter % 1000 == 0:
                    print(f"\r\tEdges added: {counter:,}", end="", flush=True)

                if counter == 1000000:
                    break

        print("\r\tFlushing transaction to database ...", end="", flush=True)
        session.commit()
        print(f"\r\tFinished adding {counter:,} edges.")

        counter = 0
        print(f"\n\nPopulating block statistics from {block_status_filename}:", flush=True)
        with open(block_status_filename, "r") as f:
            next(f)  # skip header
            for line in f:
                cols = line.strip().split(DELIMITER)
                try:
                    session.add(BlockStatus(block_height=int(cols[0]),
                                            runtime=cols[1],
                                            confirmations=int(cols[2]),
                                            bits=cols[3],
                                            difficulty=int(cols[4]),
                                            size=int(cols[5]),
                                            stripped_size=int(cols[6]),
                                            weight=int(cols[7]),
                                            block_tx_count=int(cols[8]),
                                            block_tx_inputs_count=int(cols[9]),
                                            block_tx_outputs_count=int(cols[10]),
                                            graph_generation_tx_count=int(cols[11]),
                                            graph_transfer_tx_count=int(cols[12]),
                                            graph_change_tx_count=int(cols[13]),
                                            graph_fee_tx_count=int(cols[14]),
                                            graph_generation_tx_sum=int(cols[15]),
                                            graph_transfer_tx_sum=int(cols[16]),
                                            graph_change_tx_sum=int(cols[17]),
                                            graph_fee_tx_sum=int(cols[18])))
                    session.commit()
                    session.flush()
                except Exception:
                    # TODO: this indicates a problem with the source data, maybe better log it instead.
                    pass

                counter += 1
                if counter % 1000 == 0:
                    print(f"\r\tBlock statistics added: {counter:,}", end="", flush=True)

                if counter == 1000000:
                    break

        print("\r\tFlushing transaction to database ...", end="", flush=True)
        session.commit()
        print(f"\r\tFinished adding {counter:,} block statistics.")

    print("\n\nAll process succeeded.")


if __name__ == "__main__":
    if len(sys.argv) != 4:
        print("Missing the required filenames; expect: "
              "(1) nodes filename, "
              "(2) edges filename, and "
              "(3) block status filename.")

    main(nodes_filename=sys.argv[1],
         edges_filename=sys.argv[2],
         block_status_filename=sys.argv[3])
