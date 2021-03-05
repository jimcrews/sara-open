import { useEffect, useState } from "preact/hooks";

const DatasetsInCategory = ({ datasets, category }) => {
  const [uniqueGroups, setUniqueGroups] = useState(null);
  const [datasetsInGroups, setDatasetsInGroups] = useState(null);

  // 2. get unique groups
  useEffect(() => {
    let allGroups = [];
    for (let i = 0; i < datasets.length; i++) {
      allGroups.push(datasets[i].displayGroup);
    }
    let unsortedUnique = allGroups.filter(
      (item, i, ar) => ar.indexOf(item) === i
    );
    setUniqueGroups(unsortedUnique.sort());
  }, [datasets]);

  // 3. put datasets into their groups
  useEffect(() => {
    if (uniqueGroups) {
      let datasetsObj = [];
      for (let i = 0; i < uniqueGroups.length; i++) {
        datasetsObj.push({ displayGroup: uniqueGroups[i], datasets: [] });
      }

      for (let i = 0; i < datasetsObj.length; i++) {
        for (let j = 0; j < datasets.length; j++) {
          if (datasetsObj[i].displayGroup === datasets[j].displayGroup) {
            datasetsObj[i].datasets.push(datasets[j].dataset);
          }
        }
      }

      setDatasetsInGroups(datasetsObj);
    }
  }, [uniqueGroups, datasets]);

  const categoryGroupColours = [
    "#e6ebf4",
    "#e4ffdb",
    "#ffeaea",
    "#fdffea",
    "#ecffea",
    "#eaffff",
    "#f1edff",
    "#fff4d9",
    "#edeaff",
    "#ffeafe",
    "#ffeaf0",
    "#d1c8c8",
    "#cdc8d1",
    "#e5ffba",
    "#e0fff4",
    "#ffd7d4",
    "#ffe6f4",
    "#fcffd4",
  ];

  return (
    <div className="ds-in-cat">
      {datasets.length > 0 && (
        <div>
          <div>
            <h1>{category[0].toUpperCase() + category.slice(1)} Datasets</h1>
            <span id="back-btn" onClick={() => console.log("not implemented")}>
              back
            </span>
          </div>

          {datasetsInGroups &&
            datasetsInGroups.map((group, indexGroup) => (
              <div key={group.displayGroup} className="group-div">
                {group.displayGroup ? (
                  <div className="group-header">
                    <span>--Folder icon here--</span>
                    <h3>{group.displayGroup}</h3>
                  </div>
                ) : (
                  <div style={{ marginTop: "20px" }}></div>
                )}

                <ul>
                  {group.datasets.map((ds) => (
                    <li
                      key={ds}
                      style={{
                        backgroundColor: categoryGroupColours[indexGroup],
                      }}
                      onClick={() => console.log("not implemented")}
                    >
                      {ds}
                    </li>
                  ))}
                </ul>
              </div>
            ))}
        </div>
      )}
      {datasets.length === 0 && <h1>Not Found component</h1>}
    </div>
  );
};

export default DatasetsInCategory;
