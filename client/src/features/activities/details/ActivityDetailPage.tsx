import { Grid2, Typography } from "@mui/material"
import { useParams } from "react-router";
import { useActivities } from "../../../lib/hooks/useActivities";
import ActivityHeader from "./ActivityHeader";
import ActivityInfo from "./ActivityInfo";
import ActivityChat from "./ActivityChat";
import ActivitySidebar from "./ActivitySidebar";

export default function ActivityDetailPage() {
    const { id } = useParams();
    const { activity, isLoadingActivity } = useActivities(id)

    if(isLoadingActivity) return <Typography>Loading...</Typography>

    if(!activity) return <Typography>Activity not found</Typography>

    return (
        <Grid2 container spacing={3}>
            <Grid2 size={8}>
                <ActivityHeader activity={activity} />
                <ActivityInfo activity={activity} />
                <ActivityChat />
            </Grid2>
            <Grid2 size={4}>
                <ActivitySidebar />
            </Grid2>
        </Grid2>
    )
}
