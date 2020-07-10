import React from 'react';
import {
  Router,
  Switch,
} from "react-router-dom"
import history from "./utils/history"
import PrivateRoute from "./components/PrivatePage"
import StarWars from "./components/StarWars"

function App() {
  return (
    <Router history={history}>
      <Switch>
        <PrivateRoute path="/" component={StarWars} />
      </Switch>
    </Router>
  );
}

export default App;
