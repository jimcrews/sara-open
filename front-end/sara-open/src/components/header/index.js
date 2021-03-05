import { h } from "preact";
import { Link } from "preact-router/match";
import style from "./style.css";

const Header = () => (
  <header class={style.header}>
    <h1>Preact App</h1>
    <nav>
      <Link activeClassName={style.active} href="/datasets">
        Datasets
      </Link>
      <Link activeClassName={style.active} href="/admin">
        Admin
      </Link>
    </nav>
  </header>
);

export default Header;
