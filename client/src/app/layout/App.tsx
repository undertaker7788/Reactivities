import { Box, Container, CssBaseline, Typography } from "@mui/material";
import { useState } from "react"
import NavBar from "./NavBar";
import ActivityDashboard from "../../features/activities/dashboard/ActivityDashboard";
import { useActivities } from "../../lib/hooks/useActivities";

function App() {
  const [selectedActivity, setSelectedActivity] = useState<Activity | undefined>(undefined);
  const [isEditMode, setIsEditMode] = useState(false);
  const { activities, isPending } = useActivities();

  const handleSelectedActivity = (id: string) => {
    setSelectedActivity(activities!.find(activity => activity.id === id));
  };

  const handelCancelActivity = () => {
    setSelectedActivity(undefined);
  };

  const handleOpenForm = (id?: string) => {
    if (id) handleSelectedActivity(id);
    else handelCancelActivity();

    setIsEditMode(true);
  }

  const handleFormClose = () => {
    setIsEditMode(false);
  }

  return (
    <Box sx={{ bgcolor: '#eeeeee', minHeight: '100vh' }}>
      <CssBaseline />
      <NavBar openForm={handleOpenForm} />
      <Container maxWidth='xl' sx={{ mt: 3 }}>
        {
          !activities || isPending ? (
            <Typography>Loading...</Typography>
          ) : (
            <ActivityDashboard 
              activities={activities}
              selectActivity={handleSelectedActivity}
              cancelSelectedActivity={handelCancelActivity}
              selectedActivity={selectedActivity}
              editMode={isEditMode}
              openForm={handleOpenForm}
              closeForm={handleFormClose}
            />
          )
        }
      </Container>
    </Box>
  )
}

export default App