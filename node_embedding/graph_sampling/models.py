
import sqlalchemy
from sqlalchemy import create_engine, ForeignKey, Table, Column, Integer, String, Float, Time
from sqlalchemy.orm import declarative_base
from sqlalchemy.orm import relationship
from sqlalchemy.exc import OperationalError

from urllib.parse import quote


Base = declarative_base()


# *************
# Note: do not use double-quotes for column names in SQLAlchemy
# as they create issues accessing the column (e.g., the column
# will need to be specifically named/referenced).
# *************


class Node(Base):
    __tablename__ = 'Nodes'

    id = Column(Integer, primary_key=True)
    id_generated = Column(Integer, unique=True, index=True)
    # address = Column(String(256), index=True)
    script_type = Column(Integer)


class Edge(Base):
    __tablename__ = 'Edges'

    id = Column(Integer, primary_key=True)
    source_id = Column(Integer, ForeignKey(Node.id_generated), index=True)  # TODO: is it necessary to index these columns?
    target_id = Column(Integer, ForeignKey(Node.id_generated), index=True)

    source = relationship('Node', backref='source', foreign_keys=[source_id])
    target = relationship('Node', backref='target', foreign_keys=[target_id])

    # while int may sound a better option, using double to be able to normalize the values.
    value = Column(Float)
    edge_type = Column(Float)
    time_offset = Column(Float)
    block_height = Column(Float)


class BlockStatus(Base):
    __tablename__ = "BlocksStatus"

    id = Column(Integer, primary_key=True)
    block_height = Column('BlockHeight', Integer, index=True)
    runtime = Column('Runtime', String)
    confirmations = Column('Confirmations', Integer)
    bits = Column('Bits', String(16))
    difficulty = Column('Difficulty', Integer)
    size = Column('Size', Integer)
    stripped_size = Column('StrippedSize', Integer)
    weight = Column('Weight', Integer)
    block_tx_count = Column('BlockTxCount', Integer)
    block_tx_inputs_count = Column('BlockTxInputsCount', Integer)
    block_tx_outputs_count = Column('BlockTxOutputsCount', Integer)
    graph_generation_tx_count = Column('GraphGenerationTxCount', Integer)
    graph_transfer_tx_count = Column('GraphTransferTxCount', Integer)
    graph_change_tx_count = Column('GraphChangeTxCount', Integer)
    graph_fee_tx_count = Column('GraphFeeTxCount', Integer)
    graph_generation_tx_sum = Column('GraphGenerationTxSum', Integer)
    graph_transfer_tx_sum = Column('GraphTransferTxSum', Integer)
    graph_change_tx_sum = Column('GraphChangeTxSum', Integer)
    graph_fee_tx_sum = Column('GraphFeeTxSum', Integer)


def get_engine():
    connection_url = sqlalchemy.engine.URL.create(
        drivername="postgresql+psycopg2",
        username="postgres",
        password=quote("PassWord"),
        host="localhost",
        port="5432",
        database="BC2G",
    )

    engine = create_engine(connection_url, future=True)

    try:
        engine.connect()
    except OperationalError:
        # TODO: not ideal to install a third-party library to
        #  just create a database, is not there any better approach?
        #  ideally without using a third-party library
        print(f"Database {connection_url.database} does not exist, creating it now.")
        from sqlalchemy_utils import create_database
        create_database(engine.url)
        engine.connect()
        print(f"Created database {connection_url.database}.")

    Base.metadata.create_all(engine)

    return engine

