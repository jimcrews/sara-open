import axios from "axios";

export function getCategories() {
  return new Promise(function (resolve, reject) {
    axios({
      method: "get",
      url: `${process.env.PREACT_APP_SECRET_CODE}/dwh/categories`,
    }).then(
      (response) => {
        resolve(response.data);
      },
      (error) => {
        reject(error);
      }
    );
  });
}

export function getDatasets(category) {
  return new Promise(function (resolve, reject) {
    axios({
      method: "get",
      url: `${process.env.PREACT_APP_SECRET_CODE}/dwh/datasets`,
    }).then(
      (response) => {
        resolve(response.data);
      },
      (error) => {
        reject(error);
      }
    );
  });
}
