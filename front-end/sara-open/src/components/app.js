import { h } from "preact";
import { Router } from "preact-router";
import { useEffect, useState } from "preact/hooks";
import Header from "./header";

// Code-splitting is automated for `routes` directory
import Home from "../routes/home";
import Admin from "../routes/admin";
import Datasets from "../routes/datasets";

import * as ApiMaster from "../api/master.js";

const App = () => {
  const [categories, setCategories] = useState(null);
  const [categoriesError, setCategoriesError] = useState(false);
  const [datasets, setDatasets] = useState(null);
  const [datasetsError, setDatasetsError] = useState(false);

  useEffect(() => {
    if (!categories && !categoriesError) {
      ApiMaster.getCategories()
        .then((data) => {
          console.log(data);
          setCategories(data);
        })
        .catch((err) => {
          console.log(err);
          setCategoriesError(true);
        });
    }
  }, [categories, categoriesError]);

  return (
    <div id="app">
      <Header />
      <Router>
        <Home path="/" />
        <Admin path="/admin/" categories={categories} />
        <Datasets path="/datasets/" categories={categories} />
        <Datasets path="/datasets/:category" />
      </Router>
    </div>
  );
};
export default App;
