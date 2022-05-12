import matplotlib.pyplot as plt
import pandas as pd
import seaborn as sns

filename = "../data/v3_edge_predictor_model_training.tsv"
plot_filename = "../data/v3_edge_predictor_model_training.pdf"

df = pd.read_csv(filename, index_col=0, sep="\t")

sns.set(rc={'figure.figsize': (6, 5)})
sns.set_theme()
sns.set_context("paper")
ax = sns.lineplot(data=df, x=df.index, y="loss", label="Train Loss")
ax = sns.lineplot(data=df, x=df.index, y="val_loss", label="Validation Loss")
ax.set_xlabel("Epoch")
ax.set_ylabel("Value")
ax.set(yscale="log")
# plt.legend([], [], frameon=False)  # hide legend
plt.savefig(plot_filename)
