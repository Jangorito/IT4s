df = pd.read_csv("planner_margins.csv")

plt.figure()
plt.hist(df["Margin"], bins=20)
plt.xlabel("Decision Margin")
plt.ylabel("Frequency")
plt.title("Planner Decision Margin Distribution")

plt.tight_layout()
plt.savefig("fig_planner_margins.png", dpi=300)
plt.show()