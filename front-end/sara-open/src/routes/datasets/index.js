import { h } from "preact";
import { route } from "preact-router";
import { useEffect, useState } from "preact/hooks";

import DatasetsInCategory from "./datasetsInCategory.js";
import * as ApiMaster from "../../api/master.js";

import style from "./style.css";

const Home = ({ category, categories }) => {
  const [datasets, setDatasets] = useState(null);
  const [datasetsError, setDatasetsError] = useState(false);

  useEffect(() => {
    if (category && !datasets && !datasetsError) {
      ApiMaster.getDatasets(category)
        .then((data) => {
          console.log(data);
          setDatasets(data);
        })
        .catch((err) => {
          console.log(err);
          setDatasetsError(true);
        });
    }
  }, [category]);

  return (
    <div class={style.home}>
      <h1>Datasets</h1>

      <div className="browse-section">
        <ul
          className="card-list"
          style={{ gridTemplateColumns: "repeat(4, minmax(100px, 1fr))" }}
        >
          {categories &&
            categories.map((cat) => (
              <li
                key={cat}
                className="card-item"
                onClick={() => route(`/datasets?category=${cat.toLowerCase()}`)}
              >
                {cat}
              </li>
            ))}
        </ul>
      </div>

      <div>
        <h1>Selected Category: {category}</h1>
      </div>

      {/* Browse Datasets in selected Category */}
      {datasets && category && (
        <DatasetsInCategory
          category={category}
          datasets={datasets.filter(
            (x) => x.category.toLowerCase() === category.toLowerCase()
          )}
        />
      )}
    </div>
  );
};

export default Home;
