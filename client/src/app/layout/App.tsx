import { Box, Container, CssBaseline } from "@mui/material";
import axios from "axios";
import { useState, useEffect } from "react"
import NavBar from "./NavBar";
import ActivityDashboard from "../../features/activities/dashboard/ActivityDashboard";

function App() {
  const [activities, setActivities] = useState<Activity[]>([]);
  const [selectedActivity, setSelectedActivity] = useState<Activity | undefined>(undefined);
  const [isEditMode, setIsEditMode] = useState(false);

  useEffect(() => {
    axios.get<Activity[]>("https://localhost:5001/api/activities")
      .then(response => setActivities(response.data));
  }, []);

  const handleSelectedActivity = (id: string) => {
    setSelectedActivity(activities.find(activity => activity.id === id));
  };

  const handelCancelActivity = () => {
    setSelectedActivity(undefined);
  };

  const handleDelete = (id: string) => {
    setActivities(activities.filter(x => x.id !== id));
  };

  const handleOpenForm = (id?: string) => {
    if (id) handleSelectedActivity(id);
    else handelCancelActivity();

    setIsEditMode(true);
  }

  const handleFormClose = () => {
    setIsEditMode(false);
  }

  const handleSubmitForm = (activity: Activity) => {
    if(activity.id) {
      // 傳入的 activity 已經有附帶 id 的話，就判斷這是 update
      setActivities(activities.map(x => x.id === activity.id ? activity : x));
    } else {
      // 沒有的話，就判斷這是 create
      const newActivity = { ...activity, id: activities.length.toString() };
      setSelectedActivity(newActivity);
      setActivities([...activities, newActivity]);
    }
    setIsEditMode(false);
  }

  return (
    <Box sx={{ bgcolor: '#eeeeee' }}>
      <CssBaseline />
      <NavBar openForm={handleOpenForm} />
      <Container maxWidth='xl' sx={{ mt: 3 }}>
        <ActivityDashboard 
          activities={activities}
          selectActivity={handleSelectedActivity}
          cancelSelectedActivity={handelCancelActivity}
          selectedActivity={selectedActivity}
          editMode={isEditMode}
          openForm={handleOpenForm}
          closeForm={handleFormClose}
          submitForm={handleSubmitForm}
          deleteActivity={handleDelete}
        />
      </Container>
    </Box>
  )
}

export default App