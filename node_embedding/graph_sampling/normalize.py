from models import Node, Edge, get_engine
from sqlalchemy.sql import func
from sqlalchemy.orm import Session


# TODO: this implementation is very verbose, it can be
#  simplified/more-readable if broken into multiple
#  functions, normalize abstracted, and columns min/max
#  (or any other func) sliced from a collection.


def normalize_nodes(engine):
    # Get min & max
    print("\tGetting min/max of columns ... ", end="", flush=True)
    with Session(engine) as session:
        query = session.query(func.max(Node.script_type).label("max"),
                              func.min(Node.script_type).label("min"))

        # Note that you can group query if you need to compute the min/max
        # or any other function on the groups (min/max of nodes created
        # at same block, which will return as many rows as the blocks
        # with the min/max of nodes with the same block height).
        # query = query.group_by(Node.script_type)

        query_row = query.first()
        max_, min_ = query_row[0], query_row[1]
        d = float(max_ - min_)

    print("Done!")

    # Normalize
    print("\tUpdating columns ... ", end="", flush=True)
    with Session(engine) as session:
        session.query(Node). \
            update({'script_type': (Node.script_type - min_) / d})  # 0-1 normalize
        session.commit()
    print("Done!")


def normalize_edges(engine):
    # Get min & max
    print("\tGetting min/max of columns ... ", end="", flush=True)
    with Session(engine) as session:
        query = session.query(func.max(Edge.value).label("max"),
                              func.min(Edge.value).label("min"),
                              func.max(Edge.edge_type).label("max"),
                              func.min(Edge.edge_type).label("min"),
                              func.max(Edge.time_offset).label("max"),
                              func.min(Edge.time_offset).label("min"),
                              func.max(Edge.block_height).label("max"),
                              func.min(Edge.block_height).label("min"))

        # Note that you can group query if you need to compute the min/max
        # or any other function on the groups (min/max of nodes created
        # at same block, which will return as many rows as the blocks
        # with the min/max of nodes with the same block height).
        # query = query.group_by(Node.script_type)

        query_row = query.first()
        value_max, value_min = query_row[0], query_row[1]
        value_d = float(value_max - value_min)

        edge_t_max, edge_t_min = query_row[2], query_row[3]
        edge_t_d = float(edge_t_max - edge_t_min)

        time_offset_max, time_offset_min = query_row[4], query_row[5]
        time_offset_d = float(time_offset_max - time_offset_min)

        block_h_max, block_h_min = query_row[6], query_row[7]
        block_h_d = float(block_h_max - block_h_min)

    print("Done!")

    # Normalize
    print("\tUpdating columns ... ", end="", flush=True)
    with Session(engine) as session:
        session.query(Edge). \
            update({'value':        (Edge.value - value_min) / value_d,
                    'edge_type':    (Edge.edge_type - edge_t_min) / edge_t_d,
                    'time_offset':  (Edge.time_offset - time_offset_min) / time_offset_d,
                    'block_height': (Edge.block_height - block_h_min) / block_h_d})
        session.commit()
    print("Done!")


def main():
    engine = get_engine()

    print("Normalizing nodes ... ")
    normalize_nodes(engine)
    print("Finished normalizing nodes!")

    print("Normalizing edges ... ")
    normalize_edges(engine)
    print("Finished normalizing edges!")


if __name__ == "__main__":
    main()
